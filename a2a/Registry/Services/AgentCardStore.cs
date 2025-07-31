using A2A.Models;

namespace Registry.Services
{
    public class AgentCardStore : IAgentCardStore
    {
        private readonly List<AgentCard> _agents = [];
        private readonly object _lock = new();

        public List<AgentCard> Agents
        {
            get
            {
                lock (_lock) return [.. _agents];
            }
        }

        public void Add(AgentCard agent)
        {
            lock (_lock)
            {
                if (!_agents.Any(a => a.Url == agent.Url))
                    _agents.Add(agent);
            }
        }

        public void Remove(Uri uri)
        {
            lock (_lock)
            {
                var agent = _agents.FirstOrDefault(a => a.Url == uri);
                if (agent != null) _agents.Remove(agent);
            }
        }

        public bool Contains(Uri uri)
        {
            lock (_lock) return _agents.Any(a => a.Url == uri);
        }
    }

}
