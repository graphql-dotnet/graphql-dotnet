using System.Diagnostics;
using GraphQL.Execution;
using GraphQL.Introspection;

namespace GraphQL.Tests.Language
{
    public class ShowDownTests
    {
        private string _query = @"
       query SomeDroids {
          r2d2: droid(id: ""3"") {
            ...DroidFragment
          }

          c3po: droid(id: ""4"") {
            ...DroidFragment
          }
       }
       fragment DroidFragment on Droid {
         name
       }
";
        [Fact]
        public void antlr_builder()
        {
            buildMany(new AntlrDocumentBuilder());
        }

        [Fact]
        public void core_builder()
        {
            var builder = new GraphQLDocumentBuilder();
            buildMany(builder);
        }

        private void buildMany(IDocumentBuilder builder)
        {
            var query = _query; // SchemaIntrospection.IntrospectionQuery;
            build(query, builder, 1);
            build(query, builder, 10);
            build(query, builder, 100);
            build(query, builder, 1000);
            build(query, builder, 10000);
        }

        private static void build(string query, IDocumentBuilder builder, int count)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (var i = 0; i < count; i++)
            {
                builder.Build(query);
            }

            stopwatch.Stop();

            Debug.WriteLine($"({count}) {builder.GetType().Name} Time elapsed: {stopwatch.Elapsed}");
        }
    }
}
