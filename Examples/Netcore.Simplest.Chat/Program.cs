using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Netcore.Simplest.Chat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(b =>
                {
                    b.AddLog4Net(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                })
                .UseStartup<Startup>();
    }
}
