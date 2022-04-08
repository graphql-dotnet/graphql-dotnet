using System.Diagnostics;
using GraphQL.DI;
using GraphQL.Types;
using GraphQL.Utilities;
using Xunit.Abstractions;

namespace GraphQL.Tests.Execution.Performance;

public class ListPerformanceTests : QueryTestBase<ListPerformanceSchema>
{
    public ListPerformanceTests(ITestOutputHelper output)
    {
        _output = output;

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

    public override void RegisterServices(IServiceRegister register)
    {
        register.Transient<PeopleType>();
        register.Singleton<ListPerformanceSchema>();
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
                    people {
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
            _.Schema = Schema;
            _.Query = query;
            _.Root = PeopleList;
            _.Variables = null;
            _.UserContext = null;
            _.CancellationToken = default;
            _.ValidationRules = null;
        }).ConfigureAwait(false);

        smallListTimer.Stop();

        _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

        runResult2.Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(6000 * 2); //machine specific data with a buffer
    }

    [Fact(Skip = "Benchmarks only, these numbers are machine dependant.")]
    public async Task Executes_SimpleLists_Are_Performant()
    {
        var query = @"
                query AQuery {
                    people {
                        name
                    }
                }
            ";

        var smallListTimer = new Stopwatch();

        smallListTimer.Start();

        var runResult2 = await Executer.ExecuteAsync(_ =>
        {
            _.EnableMetrics = false;
            _.Schema = Schema;
            _.Query = query;
            _.Root = PeopleList;
            _.Variables = null;
            _.UserContext = null;
            _.CancellationToken = default;
            _.ValidationRules = null;
        }).ConfigureAwait(false);

        smallListTimer.Stop();

        _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

        runResult2.Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(700 * 2); //machine specific data with a buffer
    }

    [Fact(Skip = "Benchmarks only, these numbers are machine dependant.")]
    public async Task Executes_UnionLists_Are_Performant()
    {
        var query = @"
                query AQuery {
                    people {
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
            _.EnableMetrics = false;
            _.Schema = Schema;
            _.Query = query;
            _.Root = PeopleList;
            _.Variables = null;
            _.UserContext = null;
            _.CancellationToken = default;
            _.ValidationRules = null;
        }).ConfigureAwait(false);

        smallListTimer.Stop();

        _output.WriteLine($"Total Milliseconds: {smallListTimer.ElapsedMilliseconds}");

        runResult2.Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(5600 * 2); //machine specific data with a buffer
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
    public ListPerformanceSchema(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<PeopleType>();
    }
}
