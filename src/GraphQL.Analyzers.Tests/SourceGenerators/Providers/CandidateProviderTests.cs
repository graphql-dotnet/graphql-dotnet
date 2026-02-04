using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.CandidateProviderTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/*
 * 
 * These tests do not rely on other components
 * 
 */

/// <summary>
/// Tests for CandidateProvider filtering logic.
/// These tests verify that the provider correctly identifies candidate classes based on AOT attributes.
/// Uses TestCandidateReportingGenerator to isolate testing of CandidateProvider from the full pipeline.
/// </summary>
public partial class CandidateProviderTests
{
    [Fact]
    public async Task FiltersOutClassesWithoutAttributes()
    {
        // Partial class without AOT attributes should not be matched
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public partial class MySchema : Schema
            {
            }
            """;

        // Should not generate any code because no AOT attributes
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task FiltersOutClassesWithIrrelevantAttributes()
    {
        // Partial class with non-AOT attributes should not be matched
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;
            using System;

            namespace Sample;

            [Obsolete]
            [Serializable]
            public partial class MySchema : Schema
            {
            }
            """;

        // Should not generate any code because no AOT attributes
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task IncludesPartialClassesWithSingleAotAttribute()
    {
        // Partial class with single AOT attribute should be matched
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            [AotQueryType<Query>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task IncludesPartialClassesWithMultipleAotAttributes()
    {
        // Partial class with multiple AOT attributes should be matched once (deduplicated)
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class MyInput { }

            [AotQueryType<Query>]
            [AotInputType<MyInput>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 3

            """);
    }

    [Fact]
    public async Task HandlesMultiplePartialClassesInSameFile()
    {
        // Multiple partial classes with AOT attributes should each be matched
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query1 { }
            public class Query2 { }

            [AotQueryType<Query1>]
            public partial class Schema1 : AotSchema
            {
                public Schema1() : base(null!, null!) { }
            }

            [AotQueryType<Query2>]
            public partial class Schema2 : AotSchema
            {
                public Schema2() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // Schema1
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            // Schema2
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task IncludesPartialClassWithMixedAttributes()
    {
        // Partial class with mix of AOT and non-AOT attributes should be matched
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            [Obsolete("Use new schema")]
            [AotQueryType<Query>]
            [Serializable]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 3

            """);
    }

    [Theory]
    [InlineData("[AotQueryType<Query>]")]
    [InlineData("[AotMutationType<Mutation>]")]
    [InlineData("[AotSubscriptionType<Subscription>]")]
    [InlineData("[AotInputType<MyInput>]")]
    [InlineData("[AotOutputType<MyOutput>]")]
    [InlineData("[AotGraphType<StringGraphType>]")]
    [InlineData("[AotTypeMapping<DateTime, DateTimeGraphType>]")]
    [InlineData("[AotListType<string>]")]
    [InlineData("[AotRemapType<DateGraphType, DateOnlyGraphType>]")]
    public async Task SupportsAllNineAotAttributeTypes(string attribute)
    {
        // Test that all 9 AOT attribute types are recognized
        string source =
            $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class Mutation { }
            public class Subscription { }
            public class MyInput { }
            public class MyOutput { }

            {{attribute}}
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task FiltersOutClassesNotInheritingFromAotSchema()
    {
        // Partial class with AOT attribute but not inheriting from AotSchema should be filtered out
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            [AotQueryType<Query>]
            public partial class MySchema : Schema
            {
            }
            """;

        // Should not generate any code because class doesn't inherit from AotSchema
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task IncludesClassesDirectlyInheritingFromAotSchema()
    {
        // Partial class directly inheriting from AotSchema should be included
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            [AotQueryType<Query>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task IncludesClassesIndirectlyInheritingFromAotSchema()
    {
        // Partial class indirectly inheriting from AotSchema should be included
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            public class MyBaseSchema : AotSchema
            {
                public MyBaseSchema() : base(null!, null!) { }
            }

            [AotQueryType<Query>]
            public partial class MyDerivedSchema : MyBaseSchema
            {
                public MyDerivedSchema() : base() { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MyDerivedSchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task IncludesNestedClassWhenAllContainingClassesArePartial()
    {
        // Nested partial class with AOT attribute should be included if all containing classes are partial
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            public partial class OuterClass
            {
                [AotQueryType<Query>]
                public partial class MySchema : AotSchema
                {
                    public MySchema() : base(null!, null!) { }
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample.OuterClass
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task IncludesDeeplyNestedClassWhenAllContainingClassesArePartial()
    {
        // Deeply nested partial class should be included if all containing classes are partial
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            public partial class Level1
            {
                public partial class Level2
                {
                    public partial class Level3
                    {
                        [AotQueryType<Query>]
                        public partial class MySchema : AotSchema
                        {
                            public MySchema() : base(null!, null!) { }
                        }
                    }
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample.Level1.Level2.Level3
            //   IsPartial: True
            //   AttributeCount: 1

            """);
    }

    [Fact]
    public async Task HandlesDuplicatePartialClassWithAttributesOnBothDeclarations()
    {
        // Partial class split across multiple declarations with attributes on both
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class MyInput { }

            [AotQueryType<Query>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }

            [AotInputType<MyInput>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            public partial class MySchema
            {
                public void AdditionalMethod() { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: True
            //   AttributeCount: 3

            """);
    }

    [Fact]
    public async Task IncludesNonPartialClass_WithAotAttribute()
    {
        // Non-partial class with AOT attribute is included as a candidate.
        // A diagnostic will be produced later during source generation when the generator
        // attempts to generate code for a non-partial class.
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }

            [AotQueryType<Query>]
            public class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= CandidatesReport.g.cs ============

            // Matched Candidates:

            // MySchema
            //   Namespace: Sample
            //   IsPartial: False
            //   AttributeCount: 1

            """);
    }
}
