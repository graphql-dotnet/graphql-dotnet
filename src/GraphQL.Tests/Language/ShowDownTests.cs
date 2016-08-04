using System;
using System.Diagnostics;
using GraphQL.Execution;

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

//        [Fact]
        public void antlr_builder()
        {
            buildMany(_query, new AntlrDocumentBuilder());
        }

//        [Fact]
        public void sprache_builder()
        {
            var builder = new SpracheDocumentBuilder();
            buildMany(_query, builder);
        }

        private void buildMany(string query, IDocumentBuilder builder)
        {
            build(query, builder, 100);
            build(query, builder, 1000);
            build(query, builder, 10000);
        }

        private void build(string query, IDocumentBuilder builder, int count)
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
