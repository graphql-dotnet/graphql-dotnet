using Microsoft.Extensions.DependencyInjection;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars;
using GraphQL.Validation.Complexity;
using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Complexity;

public class ComplexityMetaDataTests : IClassFixture<ComplexityMetaDataFixture>
{
    private readonly ComplexityMetaDataFixture _fixture;

    public ComplexityMetaDataTests(ComplexityMetaDataFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void MetaData_ShouldBe_Used()
    {
        var result = _fixture.Analyze(@"
query {
    hero { #2
        id #0
        name #2
        friends { #4
            id #0
            name #4
            f1: friends { #12
                name #12
            }
            f2: friends { #12
                name #12
            }
        }
    }
}
");
        result.Complexity.ShouldBe(60);
        result.TotalQueryDepth.ShouldBe(4);
    }

    [Fact]
    public void MetaData_With_Fragments_ShouldBe_Used()
    {
        var result = _fixture.Analyze(@"
query {
    hero { #2
        id #0
        name #2
        ...friendsRoot #132(4/2*66)
    }
}

fragment friendsRoot on Hero {
    friends { #3
        ...friendsIdName #27(9/2*6)
        f1: friends { #9
            name #9
        }
        f2: friends { #9
            name #9
        }
    }
}

fragment friendsIdName on Hero {
    friends { #3
        id #0
        name #3
    }
}
");
        result.Complexity.ShouldBe(136);
        result.TotalQueryDepth.ShouldBe(5);
    }
}

public class ComplexityMetaDataFixture : IDisposable
{
    public class ComplexitySchema : Schema
    {
        public ComplexitySchema()
        {
            Query = new ComplexityQuery();
        }
    }

    public class ComplexityQuery : ObjectGraphType<object>
    {
        public ComplexityQuery()
        {
            Field<Hero>("hero").WithMetadata("complexity", 2d);
        }
    }

    public class Hero : ObjectGraphType<object>
    {
        public Hero()
        {
            Field<IdGraphType>("id").WithMetadata("complexity", 0d);
            Field<StringGraphType>("name");
            Field<ListGraphType<Hero>>("friends").WithMetadata("complexity", 3d);
        }
    }

    private readonly ServiceProvider _provider;
    private readonly ComplexityAnalyzer _analyzer;
    private readonly IDocumentBuilder _documentBuilder;
    private readonly ComplexityConfiguration _config;
    private readonly ISchema _schema;

    public ComplexityMetaDataFixture()
    {
        _provider = new ServiceCollection()
            .AddSingleton<StarWarsData>()
            .AddGraphQL(builder => builder
                .AddSchema<ComplexitySchema>().AddGraphTypes()
                .AddComplexityAnalyzer()
            ).BuildServiceProvider();

        _documentBuilder = _provider.GetRequiredService<IDocumentBuilder>();
        _config = new();
        _schema = _provider.GetRequiredService<ISchema>();
        _analyzer = (ComplexityAnalyzer)_provider.GetRequiredService<IComplexityAnalyzer>();
    }

    public ComplexityResult Analyze(string query) => _analyzer.Analyze(_documentBuilder.Build(query), _config.FieldImpact ?? 2f, _config.MaxRecursionCount, _schema);

    public void Dispose() => _provider.Dispose();
}
