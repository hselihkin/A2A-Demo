using A2A.Models;

namespace Registry.Services
{
    public interface IAgentCardStore
    {
        List<AgentCard> Agents { get; }
        void Add(AgentCard agent);
        void Remove(Uri uri);
        bool Contains(Uri uri);
    }
}