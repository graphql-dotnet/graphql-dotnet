using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
