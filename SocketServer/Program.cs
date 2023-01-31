using Microsoft.Extensions.Configuration;
using System.Runtime.ConstrainedExecution;
using System;
using Microsoft.Extensions.DependencyInjection;
using SocketServer.Sockets;
using Microsoft.Extensions.Logging;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {

            #region set service provides
            var serviceProvider = ConfigureServices();
            #endregion

            #region inits
            var plainSocket = serviceProvider.GetService<ServerSocket>();
            #endregion

            new Thread(() => plainSocket.StartListening()).Start();
        }


        #region setup
        static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());

            var config = setUpConfig();
            services.AddSingleton(config);
            services.AddSingleton<ServerSocket>();
            services.AddSingleton<SocketClient>();

            return services.BuildServiceProvider();
        }

        static IConfiguration setUpConfig()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            return configuration;
        }
        #endregion
    }
}