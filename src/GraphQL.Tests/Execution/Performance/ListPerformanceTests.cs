using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conversion;
using GraphQL.Types;
using Xunit;
using Xunit.Abstractions;

namespace GraphQL.Tests.Execution.Performance
{
    public class ListPerformanceTests : QueryTestBase<ListPerformanceSchema>
    {
        public ListPerformanceTests(ITestOutputHelper output)
        {
            _output = output;

            Services.Register<PeopleType>();

            Services.Singleton(new ListPerformanceSchema(new FuncDependencyResolver(type => Services.Get(type))));

            _people = new List<Person>();

            var garfield = new Cat
            {
                Name = "Garfield",
                Meows = false
            };

            var odie = new Dog
            {
                Name = "Odie",
                Barks = true
            };

            var liz = new Person
            {
                Name = "Liz",
                Pets = new List<IPet>(),
                Friends = new List<INamed>()
            };

            for (var x = 0; x < PerformanceIterations; x++)
            {
                var person = new Person
                {
                    Name = $"Person {x}",
                    Pets = new List<IPet>
                    {
                        garfield,
                        odie
                    },
                    Friends = new List<INamed>
                    {
                        liz,
                        odie
                    }
                };

                _people.Add(person);
            }
        }

        private readonly ITestOutputHelper _output;

        private const int PerformanceIterations = 100000;
        private readonly List<Person> _people;

        private dynamic PeopleList => new
        {
            people = _people
        };

        [Fact(Skip = "Benchmarks only, these numbers are machine dependant.")]
        public async Task Executes_MultipleProperties_Are_Performant()
        {
            var query = @"
                query AQuery {
                    people{
                        name
                        name1:name
                        name2:name
                        name3:name
                        name4:name
                        name5:name
                        name6:name
                        name7:name
                        name8:name
                        name9:name
                        name10:name
                        name11:name
                        name12:name
                        name13:name
                        name14:name
                        name15:name
                        name16:name
                        name17:name
                        name18:name
                        name19:name
                    }
                }
            ";

            var smallListTimer = new Stopwatch();

            smallListTimer.Start();

            var runResult2 = await Executer.ExecuteAsync(_ =>
            {
                _.EnableMetrics = false;
                _.SetFieldMiddleware = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = PeopleList;
                _.Inputs = null;
                _.UserContext = null;
                _.CancellationToken = default(CancellationToken);
                _.ValidationRules = null;
                _.FieldNameConverter = new CamelCaseFieldNameConverter();
            });

            smallListTimer.Stop();

            _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

            Assert.Null(runResult2.Errors);
            Assert.True(smallListTimer.ElapsedMilliseconds < 6000 * 2); //machine specific data with a buffer
        }

        [Fact(Skip = "Benchmarks only, these numbers are machine dependant.")]
        // [Fact]
        public async Task Executes_SimpleLists_Are_Performant()
        {
            var query = @"
                query AQuery {
                    people{
                        name
                    }
                }
            ";

            var smallListTimer = new Stopwatch();

            smallListTimer.Start();

            var runResult2 = await Executer.ExecuteAsync(_ =>
            {
                _.SetFieldMiddleware = false;
                _.EnableMetrics = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = PeopleList;
                _.Inputs = null;
                _.UserContext = null;
                _.CancellationToken = default(CancellationToken);
                _.ValidationRules = null;
                _.FieldNameConverter = new CamelCaseFieldNameConverter();
            });

            smallListTimer.Stop();

            _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

            Assert.Null(runResult2.Errors);
            Assert.True(smallListTimer.ElapsedMilliseconds < 700 * 2); //machine specific data with a buffer
        }

        [Fact(Skip = "Benchmarks only, these numbers are machine dependant.")]
        // [Fact]
        public async Task Executes_UnionLists_Are_Performant()
        {
            var query = @"
                query AQuery {
                    people{
                      __typename
                      name
                      pets {
                        __typename
                        ... on Dog {
                          name
                          barks
                        },
                        ... on Cat {
                          name
                          meows
                        }
                      }
                    }
                }
            ";

            var smallListTimer = new Stopwatch();

            smallListTimer.Start();

            var runResult2 = await Executer.ExecuteAsync(_ =>
            {
                _.SetFieldMiddleware = false;
                _.EnableMetrics = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = PeopleList;
                _.Inputs = null;
                _.UserContext = null;
                _.CancellationToken = default(CancellationToken);
                _.ValidationRules = null;
                _.FieldNameConverter = new CamelCaseFieldNameConverter();
            });

            smallListTimer.Stop();

            _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

            Assert.Null(runResult2.Errors);
            Assert.True(smallListTimer.ElapsedMilliseconds < 5600 * 2); //machine specific data with a buffer
        }
    }

    public class PeopleType : ObjectGraphType
    {
        public PeopleType()
        {
            Name = "People";

            Field<ListGraphType<PersonType>>("people");
        }
    }

    public class ListPerformanceSchema : Schema
    {
        public ListPerformanceSchema(IDependencyResolver resolver)
            : base(resolver)
        {
            Query = resolver.Resolve<PeopleType>();
        }
    }
}
