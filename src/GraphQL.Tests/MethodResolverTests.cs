using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class MethodResolverTests
    {
        public static readonly Person Brother = new Person { Name = "brother", Age = 10 };
        public static readonly Person Sister = new Person { Name = "sister", Age = 25 };
        public static readonly Person Mother = new Person { Name = "mother", Age = 40 };
        public static readonly Person Father = new Person { Name = "father", Age = 45 };

        public static readonly Person[] Family = new[] { Brother, Mother, Sister, Father };


        [Theory]
        [InlineData(nameof(Person.NameWithAge), new[] { "separator" }, new object[] { " age=" }, "Anyone age=20")]
        [InlineData(nameof(Person.SqrAge), new string[0], new object[0], 400)]
        [InlineData(nameof(Person.Relatives), new[] { "fromAge", "toAge" }, new object[] { 0, 100 }, new[] { "brother", "mother", "sister", "father" })]
        [InlineData(nameof(Person.Relatives), new[] { "fromAge", "toAge" }, new object[] { 0, 40 }, new[] { "brother", "mother", "sister" })]
        [InlineData(nameof(Person.GetMother), new string[0], new object[0], "__mother")]
        public async Task resolve_should_work_with_methods(string methodName, string[] argNames, object[] argValues, object expected)
        {
            if (Equals(expected, "__mother"))
                expected = Mother;

            var person = new Person
            {
                Age = 20,
                Name = "Anyone"
            };
            var method = typeof(Person).GetMethod(methodName);
            var methodResolver = new MethodResolver(method);
            var ctx = new ResolveFieldContext<Person>
            {
                Source = person,
                Arguments = argNames.Select((name, i) => (name: name, value: argValues[i])).ToDictionary(_ => _.name, _ => _.value)
            };

            var actual = await methodResolver.ResolveAsync(ctx);
            if (actual is IDataLoaderResult dataLoaderResult)
            {
                actual = await dataLoaderResult.GetResultAsync();
            }

            actual.ShouldBe(expected);
        }

        public class Person
        {
            public int Age { get; set; }

            public string Name { get; set; }

            public Task<IEnumerable<string>> Relatives(int fromAge, IResolveFieldContext ctx, int toAge)
            {
                if (ctx is null)
                    throw new ArgumentNullException(nameof(ctx));

                return Task.FromResult(
                    Family
                        .Where(_ => fromAge <= _.Age && _.Age <= toAge)
                        .Select(_ => _.Name)
                );
            }

            public IDataLoaderResult<Person> GetMother(IResolveFieldContext ctx)
            {
                if (ctx is null)
                    throw new ArgumentNullException(nameof(ctx));

                return new DataLoaderContext()
                    .GetOrAddBatchLoader("test", (IEnumerable<string> names) => Task.FromResult<IDictionary<string, Person>>(new Dictionary<string, Person>() { { "Anyone", Mother } }))
                    .LoadAsync(Name);
            }

            public string NameWithAge(string separator) => Name + separator + Age;


            public double SqrAge(IResolveFieldContext ctx)
            {
                if (ctx is null)
                    throw new ArgumentNullException(nameof(ctx));

                return Age * Age;
            }

        }

    }
}
