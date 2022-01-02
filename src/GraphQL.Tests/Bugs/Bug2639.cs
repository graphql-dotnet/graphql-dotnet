using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2639
    public class Bug2639
    {
        [Fact]
        public void Serial()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Bug2639QueryType>();
            services.AddSingleton<Bug2639InterfaceType>();
            services.AddTransient<Bug2639Schema>();
            using var provider = services.BuildServiceProvider();

            for (int i = 0; i < 10; ++i)
            {
                using var schema = provider.GetRequiredService<Bug2639Schema>();
                schema.Initialize();
            }

            var query = provider.GetRequiredService<Bug2639QueryType>();
            query.ResolvedInterfaces.Count.ShouldBe(1);

            var iface = provider.GetRequiredService<Bug2639InterfaceType>();
            iface.PossibleTypes.Count.ShouldBe(1);
        }

        [Fact]
        public void Parallel()
        {
            for (int i = 0; i < 1000; ++i)
            {
                var services = new ServiceCollection();
                services.AddSingleton<Bug2639QueryType>();
                services.AddSingleton<Bug2639InterfaceType>();
                services.AddTransient<Bug2639Schema>();
                using var provider = services.BuildServiceProvider();

                const int COUNT = 5;
                var barrier = new Barrier(COUNT);

                List<Bug2639Schema> schemas = new();
                for (int c = 0; c < COUNT; ++c)
                    schemas.Add(provider.GetRequiredService<Bug2639Schema>());

                List<Task> tasks = new();
                for (int c = 0; c < COUNT; ++c)
                {
                    var copy = c;
                    tasks.Add(Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        schemas[copy].Initialize();
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                var query = provider.GetRequiredService<Bug2639QueryType>();
                var iface = provider.GetRequiredService<Bug2639InterfaceType>();

                query.ResolvedInterfaces.Count.ShouldBe(1, $"On iteration {i}");
                iface.PossibleTypes.Count.ShouldBe(1, $"On iteration {i}");

                schemas.ForEach(s => s.Dispose());
            }
        }
    }

    public sealed class Bug2639QueryType : ObjectGraphType
    {
        public Bug2639QueryType()
        {
            Field<StringGraphType>().Name("field");
            Interface<Bug2639InterfaceType>();
        }
    }

    public class Bug2639InterfaceType : InterfaceGraphType
    {
        public Bug2639InterfaceType()
        {
            Field<StringGraphType>().Name("field");
            ResolveType = _ => null;
        }
    }

    public class Bug2639Schema : Schema
    {
        public Bug2639Schema(IServiceProvider provider, Bug2639QueryType query)
            : base(provider)
        {
            Query = query;
        }
    }
}
