//using Microsoft.Extensions.AI;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

var clientTransport = new SseClientTransport(new SseClientTransportOptions
{
    Endpoint = new Uri("http://10.108.42.250:3001/sse"),
    Name = "Mcp Sse Client"
});

await using var mcpClient = await McpClientFactory.CreateAsync(clientTransport);

var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Connected to server with tools: {tool.Name}");
}