using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.TestCandidateReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for CandidateProvider filtering logic.
/// These tests verify that the provider correctly identifies candidate classes based on AOT attributes.
/// Uses TestCandidateReportingGenerator to isolate testing of CandidateProvider from the full pipeline.
/// </summary>
public class CandidateProviderTests
{
    [Fact]
    public async Task FiltersOutNonPartialClasses()
    {
        // Non-partial class with AOT attribute should not be matched
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

        // Should not generate any code because class is not partial
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

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

    [Fact]
    public async Task SupportsAllNineAotAttributeTypes()
    {
        // Test that all 9 AOT attribute types are recognized
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class Mutation { }
            public class Subscription { }
            public class MyInput { }
            public class MyOutput { }

            [AotQueryType<Query>]
            [AotMutationType<Mutation>]
            [AotSubscriptionType<Subscription>]
            [AotInputType<MyInput>]
            [AotOutputType<MyOutput>]
            [AotGraphType<StringGraphType>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            [AotListType<string>]
            [AotRemapType<DateGraphType, DateOnlyGraphType>]
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
            //   AttributeCount: 9

            """);
    }
}
