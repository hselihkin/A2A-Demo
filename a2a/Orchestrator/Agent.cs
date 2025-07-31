using A2A;
using A2A.Client;
using A2A.Client.Services;
using A2A.Models;
using A2A.Requests;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Request = Orchestrator.Models.Request;

namespace Orchestrator
{
	public class Agent
	{
		private ChatClient ChatClient { get; set; }
		private string Jwt { get; set; }
		private Uri Registry { get; set; }
		private readonly string SystemPrompt = "You are an orchestrator agent. You will receive the agent cards of all available agents containing the description of the agent, their skills and their uris. Based on the given query, decide which agent is the most appropriate and return only the uri of that agent, agent name, its query and a blank space for agent response in the format {\"url\":<uri>, \"name\":<name>, \"query\":<query>, \"response\" : \"\"} and have no other text. If multiple agents are required for the query, return them all in the same format in a list. Carefully analyse each request and break down every small operation into steps based on the available agents. Understand the capabilities of the agents from their cards properly. See what they can do and cannot do. Do not add any other text whatsoever, output just the json. If none of the agents are suitable for the given query, return the word None.";
		public Agent()
		{
			var config = new ConfigurationBuilder()
						.AddUserSecrets<Agent>()
						.Build();
			var endpoint = new Uri("https://iaia-openai.openai.azure.com/");
			var deploymentName = "gpt-4o";
			var apiKey = config["API_KEY"];
			AzureOpenAIClient azureClient = new(endpoint, new AzureKeyCredential(apiKey));
			ChatClient = azureClient.GetChatClient(deploymentName);
			Registry = new Uri(config["REGISTRY_URL"]);
		}

		public async Task<string> Router(string prompt, string jwt)
		{
			Jwt = jwt.Split()[1];

			if (prompt.Equals("list all agents", StringComparison.OrdinalIgnoreCase))
			{
				var agents = await FetchAgents();
				var formattedCards = new StringBuilder();
				foreach (var a in agents)
				{
					formattedCards.AppendLine($"Name: {a.Name}");
					formattedCards.AppendLine($"Description: {a.Description}");
					formattedCards.AppendLine($"Skills: {string.Join(", ", a.Skills)}");
					formattedCards.AppendLine($"URL: {a.Url}");
					formattedCards.AppendLine();
				}
				return formattedCards.ToString();
			}

			var resultJson = await SelectAgent(prompt);

			if (resultJson.Equals("None", StringComparison.OrdinalIgnoreCase))
				return "No matching agent found";

			Console.WriteLine(resultJson);

			List<Request> requestList = [];
			//var jsonStart = resultJson.IndexOf('[');
			//var jsonArray = resultJson[jsonStart..];
			try
			{
				var single = JsonSerializer.Deserialize<Request>(resultJson, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
				if (single != null)
					requestList.Add(single);
			}
			catch (JsonException)
			{
				requestList = JsonSerializer.Deserialize<List<Request>>(resultJson, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				}) ?? [];
			}
			try
			{
				if (requestList.Count == 0)
					return "No valid request data.";

				var responses = new List<string> { "." };
				var session = Guid.NewGuid().ToString("N");
				var agentCts = new CancellationTokenSource();

				var services = new ServiceCollection();
				services.AddA2AProtocolHttpClient(options =>
				{
					options.Endpoint = new Uri(new Uri(requestList[0].Url), "/a2a");
					options.Authorization = () => ("Bearer", Jwt);
				});

				var provider = services.BuildServiceProvider();
				var client = provider.GetRequiredService<IA2AProtocolClient>();
				var parts = new List<Part> { new TextPart(resultJson) };

				var request = new SendTaskRequest
				{
					Params = new()
					{
						Message = new()
						{
							Role = MessageRole.User,
							Parts = [.. parts]
						},
						SessionId = session
					}
				};

				var response = await client.SendTaskAsync(request, agentCts.Token);


				var content = response.Result.Artifacts
					.OfType<Artifact>()
					.SelectMany(a => a.Parts)
					.OfType<TextPart>()
					.Select(p => p.Text);

				var result = string.Join("\n", content);
				return result;

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return "Could not parse agent response.";
			}
		}

		public async Task<IReadOnlyList<AgentCard>> FetchAgents()
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Jwt);
			var discoveryDocument = await httpClient.GetA2ADiscoveryDocumentAsync(Registry);
			var agents = discoveryDocument.Agents;
			return agents;
		}
		public async Task<string> SelectAgent(string userQuery)
		{

			var agents = await FetchAgents();

			// Format all AgentCards
			var formattedCards = new StringBuilder();
			foreach (var a in agents)
			{
				formattedCards.AppendLine($"Name: {a.Name}");
				formattedCards.AppendLine($"Description: {a.Description}");
				formattedCards.AppendLine($"Skills: {string.Join(", ", a.Skills)}");
				formattedCards.AppendLine($"URL: {a.Url}");
				formattedCards.AppendLine();
			}

			var selectedUri = await Completion(userQuery, formattedCards);

			return selectedUri;
		}
		public async Task<string> Completion(string query, StringBuilder agentInfo)
		{

			var requestOptions = new ChatCompletionOptions()
			{
				MaxOutputTokenCount = 4096,
				Temperature = 0.2f,
				TopP = 1.0f,

			};

			var system = $"{SystemPrompt}\n Available agents:\n{agentInfo}";
			Console.WriteLine(system);
			List<ChatMessage> messages =
		[
			new SystemChatMessage(system),
			new UserChatMessage(query),
		];

			var response = await ChatClient.CompleteChatAsync(messages, requestOptions);
			return response.Value.Content[0].Text;
		}


	}

}