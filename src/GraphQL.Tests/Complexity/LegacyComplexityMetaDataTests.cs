using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Complexity;

public class LegacyComplexityMetaDataTests : IClassFixture<ComplexityMetaDataFixture>
{
    private readonly ComplexityMetaDataFixture _fixture;

    public LegacyComplexityMetaDataTests(ComplexityMetaDataFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MetaData_ShouldBe_Used()
    {
        var result = await _fixture.AnalyzeAsync("""
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
            """);

        result.Complexity.ShouldBe(60);
        result.TotalQueryDepth.ShouldBe(4);
    }

    [Fact]
    public async Task MetaData_With_Fragments_ShouldBe_Used()
    {
        var result = await _fixture.AnalyzeAsync("""
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
            """);

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

    public class ComplexityQuery : ObjectGraphType
    {
        public ComplexityQuery()
        {
            Field<Hero>("hero").Resolve(_ => 0)
                .ComplexityImpact(2);
        }
    }

    public class Hero : ObjectGraphType<int>
    {
        public Hero()
        {
            Field<IntGraphType>("id").Resolve(context => context.Source)
                .ComplexityImpact(0);

            Field<StringGraphType>("name").Resolve(context => $"Tom_{context.Source}");

            Field<ListGraphType<Hero>>("friends").Resolve(context => new List<int> { 0, 1, 2 }.Where(x => x != context.Source).ToList())
                .ComplexityImpact(3);
        }
    }

    private readonly ServiceProvider _provider;
    private readonly IDocumentBuilder _documentBuilder;
    private readonly LegacyComplexityConfiguration _config;
    private readonly ISchema _schema;
    private readonly IDocumentExecuter _executer;

    public ComplexityMetaDataFixture()
    {
        _provider = new ServiceCollection()
            .AddGraphQL(builder => builder
                .AddSchema<ComplexitySchema>()
                .AddLegacyComplexityAnalyzer()
            ).BuildServiceProvider();

        _documentBuilder = _provider.GetRequiredService<IDocumentBuilder>();
        _config = _provider.GetRequiredService<LegacyComplexityConfiguration>();
        _schema = _provider.GetRequiredService<ISchema>();
        _executer = _provider.GetRequiredService<IDocumentExecuter<ComplexitySchema>>();
    }

    public async Task<LegacyComplexityResult> AnalyzeAsync(string query)
    {
        var result = await _executer.ExecuteAsync(o =>
        {
            o.Query = query;
            o.RequestServices = _provider;
        }).ConfigureAwait(false);
        result.Errors.ShouldBeNull();

        return LegacyComplexityValidationRule.Analyze(_documentBuilder.Build(query), _config.FieldImpact ?? 2f, _config.MaxRecursionCount, _schema);
    }

    public void Dispose() => _provider.Dispose();
}
