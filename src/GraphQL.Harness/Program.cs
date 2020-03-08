using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace GraphQL.Harness
{
    public class Program
    {
        public static void Main(string[] args) => BuildWebHost(args).Run();

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseDefaultServiceProvider(options => options.ValidateScopes = true) // validation disabled by default
                .Build();
        }
    }
}
