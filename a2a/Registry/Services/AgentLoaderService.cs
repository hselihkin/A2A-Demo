using A2A.Models;
using Registry.Services;
using Task = System.Threading.Tasks.Task;

namespace Registry.Services
{
    public class AgentLoaderService(IServiceProvider serviceProvider, IAgentCardStore store) : IHostedService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IAgentCardStore _store = store;
        private readonly HttpClient _httpClient = new();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_store.Agents.Count > 0) return;

            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<IDbService>();
            var uris = dbService.GetAllUris();

            var tasks = uris.Select(async uri =>
            {
                try
                {
                    var card = await _httpClient.GetFromJsonAsync<AgentCard>(new Uri(uri, "/.well-known/agent.json"), cancellationToken);
                    Console.WriteLine($"Loaded {uri}");
                    if (card != null) _store.Add(card);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load agent from {uri}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}