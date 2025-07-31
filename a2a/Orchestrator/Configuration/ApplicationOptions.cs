namespace Orchestrator.Configuration
{
    public class ApplicationOptions
    {
        public AgentOptions Agent { get; set; } = new();

        public class AgentOptions
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;

            public List<Skill>? Skills { get; set; }
        }

        public class Skill
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}