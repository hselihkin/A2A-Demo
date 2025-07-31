using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server1;
using Server1.Configuration;
using Server1.Services;

Host.CreateDefaultBuilder(args)
	.ConfigureAppConfiguration((context, config) =>
	{
		config.AddJsonFile("appsettings.json", optional: false);
	})
	 .ConfigureServices((context, services) =>
	 {
		 services.Configure<ApplicationOptions>(context.Configuration.GetSection("Agent"));
	 })
	.ConfigureWebHostDefaults(webBuilder =>
	{
		webBuilder.UseStartup<Startup>();
		webBuilder.UseUrls("http://localhost:7071");
	})
	.Build()
	.Run();
