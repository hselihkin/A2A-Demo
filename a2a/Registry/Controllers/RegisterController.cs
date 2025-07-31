using A2A.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Registry.Models;
using Registry.Services;

namespace Registry.Controllers
{
    public class Request
    {
        public string Uri { get; set; } = string.Empty;
    }

    [ApiController]
    [Route(".well-known")]
    public class RegisterController(IDbService dbService, IHistoryService historyService, IAgentCardStore agentStore) : ControllerBase
    {
        private readonly IDbService _dbService = dbService;
        private readonly IHistoryService _historyService = historyService;
        private readonly IAgentCardStore _agentStore = agentStore;


        [HttpPost("register")]
        public IActionResult Register([FromBody] AgentCard agent)
        {
            if (_agentStore.Contains(agent.Url))
                return Conflict("Already registered");

            var server = new Servers
            {
                Uri = agent.Url.ToString().Trim().TrimEnd('/'),
                JoinTime = DateTime.Now
            };
            _dbService.InsertIn(server);

            var history = new History
            {
                Id = Guid.NewGuid(),
                Uri = new Uri(agent.Url.ToString().Trim().TrimEnd('/')),
                JoinTime = DateTime.Now
            };
            _historyService.InsertIn(history);

            _agentStore.Add(agent);
            return Ok("bro added");
        }

        [Authorize]
        [HttpGet("agents.json")]
        public IActionResult GetAgents()
        {
            return Ok(_agentStore.Agents);
        }

        [HttpPost("deregister")]
        public IActionResult Deregister([FromBody] Request request)
        {
            var uri = new Uri(request.Uri);
            if (!_agentStore.Contains(uri))
                return NotFound("uri doesnt exist");

            _agentStore.Remove(uri);
            _dbService.RemoveFrom(request.Uri);
            _historyService.UpdateIn(uri);

            return Ok("agent removed");
        }
    }

    [ApiController]
    [Route("signup")]
    public class TokenController(ITokenService tokenService) : ControllerBase
    {
        private readonly ITokenService _tokenSerivce = tokenService;

        [HttpPost]
        public IActionResult RequestSignup([FromBody] Request request)
        {
            var pendingKey = new ApiKey(request.Uri);
            _tokenSerivce.StorePending(pendingKey);

            return Ok(new
            {
                message = "Do you confirm registration?",
                confirmUrl = "/signup/confirm",
                tokenPreview = pendingKey.ExportJwk()
            });
        }

        [HttpPost("/signup/confirm")]
        public IActionResult ConfirmSignup([FromBody] Request request)
        {
            var key = _tokenSerivce.ConfirmPending(request.Uri);
            if (key == null)
                return NotFound("No pending request found");

            _tokenSerivce.InsertInto(key);

            return Ok(new
            {
                message = "Signup confirmed",
                jwk = key.ExportJwk()
            });
        }


    }
}
