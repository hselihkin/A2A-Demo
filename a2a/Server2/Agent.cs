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
using System.Text.Json;
using Request = Server2.Models.Request;

namespace Server2
{
    public class Agent
    {
        private ChatClient ChatClient { get; set; }
        private string Jwt { get; set; }
        private Uri Registry { get; set; }
        private readonly string SystemPrompt = "You are a math agent. You can perform simple addition, subtraction, multiplicaiton and division. You cannot perform any other operation. The query may or may not contain context. If context is available, then use the information in the context to answer the query.";
        public Agent()
        {
            var config = new ConfigurationBuilder()
                        .AddUserSecrets<Agent>()
                        .Build();
            var endpoint = new Uri("https://iaia-openai.openai.azure.com/");
            var deploymentName = "gpt-4o";
            var apiKey = config["API-KEY"];
            AzureOpenAIClient azureClient = new(endpoint, new AzureKeyCredential(apiKey));
            ChatClient = azureClient.GetChatClient(deploymentName);
        }
        public string AddContext(List<Request> json, int index)
        {
            string context = string.Empty;
            for (int i = 0; i < index; i++)
            {
                context += json[i].Response;
            }
            return context;
        }

        public async Task<string> Router(List<Request> json)
        {
            int selectedStep = SelectStep(json);
            if (selectedStep == -1)
            {
                return json[^1].Response;
            }
            else
            {
                var uri = json[selectedStep].Url;
                var session = Guid.NewGuid().ToString("N");
                var agentCts = new CancellationTokenSource();
                Console.WriteLine(uri);
                var service = new ServiceCollection();
                service.AddA2AProtocolHttpClient(options =>
                {
                    options.Endpoint = new Uri(new Uri(uri), "/a2a");
                    options.Authorization = null;
                });
                var provider = service.BuildServiceProvider();
                var client = provider.GetRequiredService<IA2AProtocolClient>();
                var jsonstring = JsonSerializer.Serialize(json);
                Console.WriteLine("json string", jsonstring);
                var parts = new List<Part>() { new TextPart(jsonstring) };
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
                try
                {
                    var response = await client.SendTaskAsync(request, agentCts.Token);
                    var content = response.Result.Artifacts
                    .OfType<Artifact>()
                    .SelectMany(a => a.Parts)
                    .OfType<TextPart>()
                    .Select(p => p.Text)
                    .ToList();

                    return string.Join("\n", content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return "No artifacts found";
                }
            }
        }
        public int SelectStep(List<Request> request)
        {
            int result = -1;
            for (int i = 0; i < request.Count; i++)
            {
                if (request[i].Response == "")
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
        public async Task<string> Workflow(string query)
        {
            if (query.StartsWith('['))
            {
                var result = ParseJsonList(query);
                var selectedStep = SelectStep(result);
                var context = AddContext(result, selectedStep);
                var queryString = "Context : " + context + "\n\nQuery : " + result[selectedStep].Query;
                var response = await Completion(queryString);
                result[selectedStep].Response = response;
                var final = await Router(result);
                return final;
            }
            else
            {
                var result = ParseJson(query);
                var response = await Completion(result.Query);
                return response;
            }
        }

        public Request ParseJson(string json)
        {
            var result = JsonSerializer.Deserialize<Request>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result;
        }

        public List<Request> ParseJsonList(string json)
        {
            var result = JsonSerializer.Deserialize<List<Request>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }

        public async Task<string> Completion(string query)
        {

            var requestOptions = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = 4096,
                Temperature = 0.2f,
                TopP = 1.0f,

            };
            Console.WriteLine(query);
            List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(query),
        ];

            var response = await ChatClient.CompleteChatAsync(messages, requestOptions);
            return response.Value.Content[0].Text;
        }


    }

}

