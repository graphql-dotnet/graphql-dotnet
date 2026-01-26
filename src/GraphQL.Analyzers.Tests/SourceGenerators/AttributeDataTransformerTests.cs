using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.AttributeDataTransformerTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for AttributeDataTransformer transformation logic.
/// These tests verify that the transformer correctly extracts and categorizes AOT attributes.
/// Uses TestAttributeDataReportingGenerator to isolate testing of AttributeDataTransformer from the full pipeline.
/// </summary>
public partial class AttributeDataTransformerTests
{
    [Fact]
    public async Task TransformsSingleQueryTypeAttribute()
    {
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

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] Query (CLR)
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsSingleMutationTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Mutation { }

            [AotMutationType<Mutation>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 1
            //   [0] Mutation (CLR)
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsSingleSubscriptionTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Subscription { }

            [AotSubscriptionType<Subscription>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 1
            //   [0] Subscription (CLR)
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsQueryTypeWithGraphType()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class QueryGraphType : ObjectGraphType
            {
            }

            [AotQueryType<QueryGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] QueryGraphType (GraphType)
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsOutputTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product { }

            [AotOutputType<Product>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 1
            //   [0] Product
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsInputTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class CreateProductInput { }

            [AotInputType<CreateProductInput>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 1
            //   [0] CreateProductInput
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsGraphTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AotGraphType<StringGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 1
            //   [0] StringGraphType
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsTypeMappingAttribute()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

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

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 1
            //   [0] DateTime -> DateTimeGraphType
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsListTypeAttribute()
    {
        const string source =
            """
            using System.Collections.Generic;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AotListType<List<string>>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 1
            //   [0] List<string>
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsRemapTypeAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

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

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 1
            //   [0] DateGraphType -> DateOnlyGraphType

            """);
    }

    [Fact]
    public async Task TransformsAllNineAttributeTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class Mutation { }
            public class Subscription { }
            public class Product { }
            public class CreateProductInput { }

            [AotQueryType<Query>]
            [AotMutationType<Mutation>]
            [AotSubscriptionType<Subscription>]
            [AotOutputType<Product>]
            [AotInputType<CreateProductInput>]
            [AotGraphType<StringGraphType>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            [AotListType<List<string>>]
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

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] Query (CLR)
            //
            // MutationTypes: 1
            //   [0] Mutation (CLR)
            //
            // SubscriptionTypes: 1
            //   [0] Subscription (CLR)
            //
            // OutputTypes: 1
            //   [0] Product
            //
            // InputTypes: 1
            //   [0] CreateProductInput
            //
            // GraphTypes: 1
            //   [0] StringGraphType
            //
            // TypeMappings: 1
            //   [0] DateTime -> DateTimeGraphType
            //
            // ListTypes: 1
            //   [0] List<string>
            //
            // RemapTypes: 1
            //   [0] DateGraphType -> DateOnlyGraphType

            """);
    }

    [Fact]
    public async Task TransformsMultipleAttributesOfSameType()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product { }
            public class Order { }
            public class Customer { }

            [AotOutputType<Product>]
            [AotOutputType<Order>]
            [AotOutputType<Customer>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 3
            //   [0] Product
            //   [1] Order
            //   [2] Customer
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task TransformsMultipleTypeMappings()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AotTypeMapping<DateTime, DateTimeGraphType>]
            [AotTypeMapping<Guid, IdGraphType>]
            [AotTypeMapping<TimeSpan, TimeSpanSecondsGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 0
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 3
            //   [0] DateTime -> DateTimeGraphType
            //   [1] Guid -> IdGraphType
            //   [2] TimeSpan -> TimeSpanSecondsGraphType
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesAttributesAcrossPartialDeclarations()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class Product { }

            [AotQueryType<Query>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }

            [AotOutputType<Product>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            public partial class MySchema
            {
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] Query (CLR)
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 1
            //   [0] Product
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 1
            //   [0] DateTime -> DateTimeGraphType
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task IgnoresNonAotAttributes()
    {
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

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] Query (CLR)
            //
            // MutationTypes: 0
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesEmptySchemaWithNoAotAttributes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        // Should not generate any code because no AOT attributes
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task TransformsMixedClrAndGraphTypeRootTypes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query { }
            public class MutationGraphType : ObjectGraphType { }

            [AotQueryType<Query>]
            [AotMutationType<MutationGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeDataReport.g.cs ============

            // Schema: MySchema
            //
            // QueryTypes: 1
            //   [0] Query (CLR)
            //
            // MutationTypes: 1
            //   [0] MutationGraphType (GraphType)
            //
            // SubscriptionTypes: 0
            //
            // OutputTypes: 0
            //
            // InputTypes: 0
            //
            // GraphTypes: 0
            //
            // TypeMappings: 0
            //
            // ListTypes: 0
            //
            // RemapTypes: 0

            """);
    }
}
