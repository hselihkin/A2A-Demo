using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server3;
using Server3.Configurations;
using Server3.Services;

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
        webBuilder.UseUrls("http://localhost:7074");
    })
    .Build()
    .Run();
