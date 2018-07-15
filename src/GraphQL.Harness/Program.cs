using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GraphQL.Harness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var directory = Directory.GetCurrentDirectory();

            return WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(directory)
                .UseWebRoot(Path.Combine(directory, "public"))
                .UseStartup<Startup>()
                .Build();
        }
    }
}
