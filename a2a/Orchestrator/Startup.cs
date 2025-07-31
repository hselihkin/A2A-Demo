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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Orchestrator.Services;
using Orchestrator.Configuration;

namespace Orchestrator
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

			services.AddHttpContextAccessor();

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
					.WithUrl(new Uri("http://localhost:7073"))
					.SupportsStreaming();

				if (options.Agent.Skills != null)
				{
					foreach (var skill in options.Agent.Skills)
					{
						builder.WithSkill(skillBuilder => skillBuilder
							.WithId(skill.Id)
							.WithName(skill.Name)
							.WithDescription(skill.Description!));
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

		public static void Configure(IApplicationBuilder app)
		{
			app.UseRouting();

			app.MapA2AWellKnownAgentEndpoint();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapA2AHttpEndpoint("/a2a");
			});
		}

	}
}