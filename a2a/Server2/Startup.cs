using A2A.Models;
using A2A.Server;
using A2A.Server.AspNetCore;
using A2A.Server.Infrastructure;
using A2A.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Server2.Configuration;
using Server2.Services;
using Server2;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server2
{
    public class Startup(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOptions<ApplicationOptions>()
                .Bind(_configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            });

            services.AddDistributedMemoryCache();
            services.AddSingleton<Agent>();

            services.AddTransient<IAgentRuntime, AgentRuntime>();

            services.AddA2AWellKnownAgent((provider, builder) =>
            {
                var options = provider.GetRequiredService<IOptions<ApplicationOptions>>().Value;
                builder
                    .WithName(options.Agent.Name)
                    .WithDescription(options.Agent.Description!)
                    .WithVersion(options.Agent.Version)
                    .WithProvider(p => p
                        .WithOrganization("a2a-net")
                        .WithUrl(new("https://github.com/neuroglia-io/a2a-net")))
                    .WithUrl(new Uri("http://localhost:7072"))
                    .SupportsStreaming();

                if (options.Agent.Skills != null)
                {
                    foreach (var skill in options.Agent.Skills)
                    {
                        builder.WithSkill(skillBuilder => skillBuilder
                            .WithId(skill.Id)
                            .WithName(skill.Name)
                            .WithDescription(skill.Description!));
                        //.WithInputMode(skill.DefaultInputMode)
                        //.WithOutputMode(skill.DefaultOutputMode));
                    }
                }
            });

            services.AddA2AProtocolServer(builder =>
            {
                builder
                    .UseAgentRuntime(provider => provider.GetRequiredService<IAgentRuntime>())
                    .UseDistributedCacheTaskRepository()
                    .SupportsStreaming();
            });
        }

        public static async void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IHttpClientFactory clientFactory)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Startup>().Build();
            app.UseRouting();
            app.MapA2AWellKnownAgentEndpoint();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapA2AHttpEndpoint("/a2a");
            });

            var agentUrl = "http://localhost:7072";
            var registryBase = config["REGISTRY_URL"];
            var token = config["REGISTRY_TOKEN"];
            if (token == null)
            {

                var httpClient = clientFactory.CreateClient();

                var signupResponse = await httpClient.PostAsJsonAsync($"{registryBase}/signup", new { Uri = agentUrl });

                if (!signupResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to initiate signup: {signupResponse.StatusCode}");
                    return;
                }

                Console.WriteLine("Signup initiated. Confirming...");

                var confirmResponse = await httpClient.PostAsJsonAsync($"{registryBase}/signup/confirm", new { Uri = agentUrl });

                if (!confirmResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to confirm signup: {confirmResponse.StatusCode}");
                    return;
                }

                var jwkResponse = await confirmResponse.Content.ReadFromJsonAsync<JsonElement>();
                string jwkString = jwkResponse.GetProperty("jwk").GetString();
                using var jwkDoc = JsonDocument.Parse(jwkString);
                token = jwkDoc.RootElement.GetProperty("k").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("Key not found in response");
                    return;
                }
                Console.WriteLine(token);
                Console.WriteLine("Signup complete. Received key.");
            }

            lifetime.ApplicationStopping.Register(() =>
            {
                var shutdownClient = clientFactory.CreateClient();
                shutdownClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var deregisterUrl = $"{registryBase}/.well-known/deregister";
                var payload = new { Uri = agentUrl };
                var result = shutdownClient.PostAsJsonAsync(deregisterUrl, payload).GetAwaiter().GetResult();

                if (result.IsSuccessStatusCode)
                    Console.WriteLine("Agent deregistered");
                else
                    Console.WriteLine($"Failed to deregister: {result.StatusCode}");
            });

            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            var scopedClient = services.GetRequiredService<IHttpClientFactory>().CreateClient();
            scopedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var agentCardResponse = await scopedClient.GetAsync($"{agentUrl}/.well-known/agent.json");
            if (!agentCardResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch agent card: {agentCardResponse.StatusCode}");
                return;
            }

            var agentCard = await agentCardResponse.Content.ReadFromJsonAsync<AgentCard>();

            if (agentCard is null)
            {
                Console.WriteLine("AgentCard deserialization failed.");
                return;
            }

            var registerUrl = $"{registryBase}/.well-known/register";
            var registrationResult = await scopedClient.PostAsJsonAsync(registerUrl, agentCard);

            if (registrationResult.IsSuccessStatusCode)
                Console.WriteLine("Agent registered to registry");
            else
                Console.WriteLine($"Failed to register agent: {registrationResult.StatusCode}");
        }
    }
}