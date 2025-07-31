using A2A.Models;
using A2A.Server.Infrastructure;
using A2A.Server.Infrastructure.Services;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Task = System.Threading.Tasks.Task;

namespace Server2.Services
{
    public class AgentRuntime(Agent a) : IAgentRuntime
    {
        private readonly Agent agent = a;

        private ConcurrentDictionary<string, CancellationTokenSource> Tasks { get; } = [];
        public async IAsyncEnumerable<AgentResponseContent> ExecuteAsync(TaskRecord task, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Tasks[task.Id] = linkedCts;

            var userInput = task.Message.Parts?.FirstOrDefault()?.ToText() ?? string.Empty;
            Console.WriteLine(userInput);
            var result = await agent.Workflow(userInput);

            yield return new AgentResponseContent(new Artifact
            {
                Index = 0,
                Parts = [new TextPart(result)],
                LastChunk = true
            });

            Tasks.TryRemove(task.Id, out _);
        }

        public Task CancelAsync(string taskId, CancellationToken cancellationToken = default)
        {
            if (Tasks.TryRemove(taskId, out var cts))
            {
                cts.Cancel();
            }
            return Task.CompletedTask;
        }
    }
}