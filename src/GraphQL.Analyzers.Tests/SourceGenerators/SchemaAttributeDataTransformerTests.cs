using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.SchemaAttributeDataTransformerTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for SchemaAttributeDataTransformer's type graph walking and discovery logic.
/// These tests verify that the transformer correctly discovers all types referenced through
/// the type graph, creates appropriate CLR-to-GraphType mappings, and tracks input list types.
/// </summary>
public partial class SchemaAttributeDataTransformerTests
{
    [Fact]
    public async Task TransformsSimpleQueryType()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query
            {
                public string Hello() => "World";
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 2
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] StringGraphType
            //
            // OutputClrTypeMappings: 2
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] string -> StringGraphType
            //
            // InputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputListTypes: 0

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
                public QueryGraphType()
                {
                    Field<StringGraphType>("hello");
                }
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: QueryGraphType
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] QueryGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverTypesReferencedByQueryClass()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 4
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<Product>
            //   [2] StringGraphType
            //   [3] DecimalGraphType
            //
            // OutputClrTypeMappings: 4
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Product -> AutoRegisteringObjectGraphType<Product>
            //   [2] string -> StringGraphType
            //   [3] decimal -> DecimalGraphType
            //
            // InputClrTypeMappings: 2
            //   [0] string -> StringGraphType
            //   [1] decimal -> DecimalGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverNestedTypesInTypeGraph()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class Customer
            {
                public string Name { get; set; }
                public Address Address { get; set; }
            }

            public class Query
            {
                public Customer GetCustomer() => new Customer();
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 4
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<Customer>
            //   [2] StringGraphType
            //   [3] AutoRegisteringObjectGraphType<Address>
            //
            // OutputClrTypeMappings: 4
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Customer -> AutoRegisteringObjectGraphType<Customer>
            //   [2] string -> StringGraphType
            //   [3] Address -> AutoRegisteringObjectGraphType<Address>
            //
            // InputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesInputTypesAndTracksInTypeGraph()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class CreateProductInput
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            public class Product
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            public class Mutation
            {
                public Product CreateProduct(CreateProductInput input) => new Product();
            }

            [AotMutationType<Mutation>]
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: AutoRegisteringObjectGraphType<Mutation>
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 5
            //   [0] AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] AutoRegisteringObjectGraphType<Mutation>
            //   [2] StringGraphType
            //   [3] DecimalGraphType
            //   [4] AutoRegisteringObjectGraphType<Product>
            //
            // OutputClrTypeMappings: 4
            //   [0] Mutation -> AutoRegisteringObjectGraphType<Mutation>
            //   [1] string -> StringGraphType
            //   [2] decimal -> DecimalGraphType
            //   [3] Product -> AutoRegisteringObjectGraphType<Product>
            //
            // InputClrTypeMappings: 3
            //   [0] CreateProductInput -> AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] string -> StringGraphType
            //   [2] decimal -> DecimalGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesTypeMappingAttributes()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Query
            {
                public DateTime GetCurrentTime() => DateTime.Now;
            }

            [AotQueryType<Query>]
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] AutoRegisteringObjectGraphType<Query>
            //
            // OutputClrTypeMappings: 2
            //   [0] DateTime -> DateTimeGraphType
            //   [1] Query -> AutoRegisteringObjectGraphType<Query>
            //
            // InputClrTypeMappings: 1
            //   [0] DateTime -> DateTimeGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesExplicitGraphTypeAttributes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class ProductGraphType : ObjectGraphType
            {
                public ProductGraphType()
                {
                    Field<StringGraphType>("name");
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesGraphTypeWithSourceType()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class ProductGraphType : ObjectGraphType<Product>
            {
                public ProductGraphType()
                {
                    Field(x => x.Name);
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 1
            //   [0] Product -> ProductGraphType
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesListTypesFromAttributes()
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 0
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 1
            //   [0] List<string>

            """);
    }

    [Fact]
    public async Task DiscoverListTypesFromInputTypeProperties()
    {
        const string source =
            """
            using System.Collections.Generic;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class CreateOrderInput
            {
                public List<string> Tags { get; set; }
                public List<int> Quantities { get; set; }
            }

            [AotInputType<CreateOrderInput>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringInputObjectGraphType<CreateOrderInput>
            //   [1] StringGraphType
            //   [2] IntGraphType
            //
            // OutputClrTypeMappings: 2
            //   [0] string -> StringGraphType
            //   [1] int -> IntGraphType
            //
            // InputClrTypeMappings: 3
            //   [0] CreateOrderInput -> AutoRegisteringInputObjectGraphType<CreateOrderInput>
            //   [1] string -> StringGraphType
            //   [2] int -> IntGraphType
            //
            // InputListTypes: 2
            //   [0] List<string>
            //   [1] List<int>

            """);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task HandlesInterfaceTypes(bool explicitInterface, bool isClass)
    {
        string source =
            $$"""
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public {{(isClass ? "class" : "interface")}} IProduct
            {
                public string Name { get; }
            }

            public class Query
            {
                public IProduct GetProduct() => null;
            }

            [AotQueryType<Query>]
            [AotOutputType<IProduct>({{(explicitInterface ? "Kind = OutputTypeKind.Interface" : "")}})]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // All three test cases should produce AutoRegisteringInterfaceGraphType:
        // 1. (true, true): class with explicit Kind=Interface
        // 2. (true, false): interface with explicit Kind=Interface
        // 3. (false, false): interface without Kind (auto-detected as interface)
        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringInterfaceGraphType<IProduct>
            //   [1] AutoRegisteringObjectGraphType<Query>
            //   [2] StringGraphType
            //
            // OutputClrTypeMappings: 3
            //   [0] IProduct -> AutoRegisteringInterfaceGraphType<IProduct>
            //   [1] Query -> AutoRegisteringObjectGraphType<Query>
            //   [2] string -> StringGraphType
            //
            // InputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesComplexTypeGraphWithMultipleRootTypes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

            public class CreateProductInput
            {
                public string Name { get; set; }
            }

            public class Mutation
            {
                public Product CreateProduct(CreateProductInput input) => new Product();
            }

            public class Subscription
            {
                public Product OnProductCreated() => new Product();
            }

            [AotQueryType<Query>]
            [AotMutationType<Mutation>]
            [AotSubscriptionType<Subscription>]
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: AutoRegisteringObjectGraphType<Mutation>
            //
            // SubscriptionRootGraphType: AutoRegisteringObjectGraphType<Subscription>
            //
            // DiscoveredGraphTypes: 6
            //   [0] AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] AutoRegisteringObjectGraphType<Query>
            //   [2] AutoRegisteringObjectGraphType<Mutation>
            //   [3] AutoRegisteringObjectGraphType<Subscription>
            //   [4] StringGraphType
            //   [5] AutoRegisteringObjectGraphType<Product>
            //
            // OutputClrTypeMappings: 5
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Mutation -> AutoRegisteringObjectGraphType<Mutation>
            //   [2] Subscription -> AutoRegisteringObjectGraphType<Subscription>
            //   [3] string -> StringGraphType
            //   [4] Product -> AutoRegisteringObjectGraphType<Product>
            //
            // InputClrTypeMappings: 2
            //   [0] CreateProductInput -> AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverNestedInputTypes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class AddressInput
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class CreateCustomerInput
            {
                public string Name { get; set; }
                public AddressInput Address { get; set; }
            }

            [AotInputType<CreateCustomerInput>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringInputObjectGraphType<CreateCustomerInput>
            //   [1] StringGraphType
            //   [2] AutoRegisteringInputObjectGraphType<AddressInput>
            //
            // OutputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputClrTypeMappings: 3
            //   [0] CreateCustomerInput -> AutoRegisteringInputObjectGraphType<CreateCustomerInput>
            //   [1] string -> StringGraphType
            //   [2] AddressInput -> AutoRegisteringInputObjectGraphType<AddressInput>
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesGraphTypeWithAutoRegisterClrMappingFalse()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class ProductGraphType : ObjectGraphType<Product>
            {
                public ProductGraphType()
                {
                    Field(x => x.Name);
                }
            }

            [AotGraphType<ProductGraphType>(AutoRegisterClrMapping = false)]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task AvoidsDuplicateTypeDiscovery()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
                public Product GetAnotherProduct() => new Product();
            }

            [AotQueryType<Query>]
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringObjectGraphType<Product>
            //   [1] AutoRegisteringObjectGraphType<Query>
            //   [2] StringGraphType
            //
            // OutputClrTypeMappings: 3
            //   [0] Product -> AutoRegisteringObjectGraphType<Product>
            //   [1] Query -> AutoRegisteringObjectGraphType<Query>
            //   [2] string -> StringGraphType
            //
            // InputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ProcessesAutoRegisteringGraphTypes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            [AotGraphType<AutoRegisteringObjectGraphType<Product>>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 2
            //   [0] AutoRegisteringObjectGraphType<Product>
            //   [1] StringGraphType
            //
            // OutputClrTypeMappings: 2
            //   [0] Product -> AutoRegisteringObjectGraphType<Product>
            //   [1] string -> StringGraphType
            //
            // InputClrTypeMappings: 1
            //   [0] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesEmptySchemaWithNoTypes()
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
    public async Task CombinesTypeMappingWithTypeDiscovery()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

            [AotQueryType<Query>]
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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<Product>
            //   [2] StringGraphType
            //
            // OutputClrTypeMappings: 4
            //   [0] DateTime -> DateTimeGraphType
            //   [1] Query -> AutoRegisteringObjectGraphType<Query>
            //   [2] Product -> AutoRegisteringObjectGraphType<Product>
            //   [3] string -> StringGraphType
            //
            // InputClrTypeMappings: 2
            //   [0] DateTime -> DateTimeGraphType
            //   [1] string -> StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverGraphTypesReferencedInTypeGraph()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class SpecialStringGraphType : StringGraphType
            {
            }

            public class Product
            {
                [OutputType(typeof(SpecialStringGraphType))]
                public string Name { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 3
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<Product>
            //   [2] SpecialStringGraphType
            //
            // OutputClrTypeMappings: 2
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Product -> AutoRegisteringObjectGraphType<Product>
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverBuiltInScalarTypesInOutputTypes()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
                public DateTime CreatedAt { get; set; }
                public Guid Id { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 7
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<Product>
            //   [2] StringGraphType
            //   [3] IntGraphType
            //   [4] DecimalGraphType
            //   [5] DateTimeGraphType
            //   [6] IdGraphType
            //
            // OutputClrTypeMappings: 7
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Product -> AutoRegisteringObjectGraphType<Product>
            //   [2] string -> StringGraphType
            //   [3] int -> IntGraphType
            //   [4] decimal -> DecimalGraphType
            //   [5] DateTime -> DateTimeGraphType
            //   [6] Guid -> IdGraphType
            //
            // InputClrTypeMappings: 5
            //   [0] string -> StringGraphType
            //   [1] int -> IntGraphType
            //   [2] decimal -> DecimalGraphType
            //   [3] DateTime -> DateTimeGraphType
            //   [4] Guid -> IdGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DiscoverBuiltInScalarTypesInInputTypes()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class CreateProductInput
            {
                public string Name { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
                public DateTime CreatedAt { get; set; }
                public bool IsActive { get; set; }
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 6
            //   [0] AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] StringGraphType
            //   [2] IntGraphType
            //   [3] DecimalGraphType
            //   [4] DateTimeGraphType
            //   [5] BooleanGraphType
            //
            // OutputClrTypeMappings: 5
            //   [0] string -> StringGraphType
            //   [1] int -> IntGraphType
            //   [2] decimal -> DecimalGraphType
            //   [3] DateTime -> DateTimeGraphType
            //   [4] bool -> BooleanGraphType
            //
            // InputClrTypeMappings: 6
            //   [0] CreateProductInput -> AutoRegisteringInputObjectGraphType<CreateProductInput>
            //   [1] string -> StringGraphType
            //   [2] int -> IntGraphType
            //   [3] decimal -> DecimalGraphType
            //   [4] DateTime -> DateTimeGraphType
            //   [5] bool -> BooleanGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task BuiltInScalarsRegisteredInBothInputAndOutputMappings()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public long Id { get; set; }
                public double Rating { get; set; }
            }

            public class UpdateProductInput
            {
                public long Id { get; set; }
                public double Rating { get; set; }
            }

            public class Query
            {
                public Product GetProduct() => new Product();
            }

            public class Mutation
            {
                public Product UpdateProduct(UpdateProductInput input) => new Product();
            }

            [AotQueryType<Query>]
            [AotMutationType<Mutation>]
            [AotInputType<UpdateProductInput>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: AutoRegisteringObjectGraphType<Mutation>
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 6
            //   [0] AutoRegisteringInputObjectGraphType<UpdateProductInput>
            //   [1] AutoRegisteringObjectGraphType<Query>
            //   [2] AutoRegisteringObjectGraphType<Mutation>
            //   [3] LongGraphType
            //   [4] FloatGraphType
            //   [5] AutoRegisteringObjectGraphType<Product>
            //
            // OutputClrTypeMappings: 5
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] Mutation -> AutoRegisteringObjectGraphType<Mutation>
            //   [2] long -> LongGraphType
            //   [3] double -> FloatGraphType
            //   [4] Product -> AutoRegisteringObjectGraphType<Product>
            //
            // InputClrTypeMappings: 3
            //   [0] UpdateProductInput -> AutoRegisteringInputObjectGraphType<UpdateProductInput>
            //   [1] long -> LongGraphType
            //   [2] double -> FloatGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task AllPrimitiveScalarTypesAreRegistered()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class AllScalarsType
            {
                public int IntValue { get; set; }
                public long LongValue { get; set; }
                public double DoubleValue { get; set; }
                public float FloatValue { get; set; }
                public decimal DecimalValue { get; set; }
                public string StringValue { get; set; }
                public bool BoolValue { get; set; }
                public DateTime DateTimeValue { get; set; }
                public DateTimeOffset DateTimeOffsetValue { get; set; }
                public TimeSpan TimeSpanValue { get; set; }
                public Guid GuidValue { get; set; }
                public short ShortValue { get; set; }
                public ushort UShortValue { get; set; }
                public ulong ULongValue { get; set; }
                public uint UIntValue { get; set; }
                public byte ByteValue { get; set; }
                public sbyte SByteValue { get; set; }
                public Uri UriValue { get; set; }
            }

            public class Query
            {
                public AllScalarsType GetAllScalars() => new AllScalarsType();
            }

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

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: AutoRegisteringObjectGraphType<Query>
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 19
            //   [0] AutoRegisteringObjectGraphType<Query>
            //   [1] AutoRegisteringObjectGraphType<AllScalarsType>
            //   [2] IntGraphType
            //   [3] LongGraphType
            //   [4] FloatGraphType
            //   [5] DecimalGraphType
            //   [6] StringGraphType
            //   [7] BooleanGraphType
            //   [8] DateTimeGraphType
            //   [9] DateTimeOffsetGraphType
            //   [10] TimeSpanSecondsGraphType
            //   [11] IdGraphType
            //   [12] ShortGraphType
            //   [13] UShortGraphType
            //   [14] ULongGraphType
            //   [15] UIntGraphType
            //   [16] ByteGraphType
            //   [17] SByteGraphType
            //   [18] UriGraphType
            //
            // OutputClrTypeMappings: 20
            //   [0] Query -> AutoRegisteringObjectGraphType<Query>
            //   [1] AllScalarsType -> AutoRegisteringObjectGraphType<AllScalarsType>
            //   [2] int -> IntGraphType
            //   [3] long -> LongGraphType
            //   [4] double -> FloatGraphType
            //   [5] float -> FloatGraphType
            //   [6] decimal -> DecimalGraphType
            //   [7] string -> StringGraphType
            //   [8] bool -> BooleanGraphType
            //   [9] DateTime -> DateTimeGraphType
            //   [10] DateTimeOffset -> DateTimeOffsetGraphType
            //   [11] TimeSpan -> TimeSpanSecondsGraphType
            //   [12] Guid -> IdGraphType
            //   [13] short -> ShortGraphType
            //   [14] ushort -> UShortGraphType
            //   [15] ulong -> ULongGraphType
            //   [16] uint -> UIntGraphType
            //   [17] byte -> ByteGraphType
            //   [18] sbyte -> SByteGraphType
            //   [19] Uri -> UriGraphType
            //
            // InputClrTypeMappings: 18
            //   [0] int -> IntGraphType
            //   [1] long -> LongGraphType
            //   [2] double -> FloatGraphType
            //   [3] float -> FloatGraphType
            //   [4] decimal -> DecimalGraphType
            //   [5] string -> StringGraphType
            //   [6] bool -> BooleanGraphType
            //   [7] DateTime -> DateTimeGraphType
            //   [8] DateTimeOffset -> DateTimeOffsetGraphType
            //   [9] TimeSpan -> TimeSpanSecondsGraphType
            //   [10] Guid -> IdGraphType
            //   [11] short -> ShortGraphType
            //   [12] ushort -> UShortGraphType
            //   [13] ulong -> ULongGraphType
            //   [14] uint -> UIntGraphType
            //   [15] byte -> ByteGraphType
            //   [16] sbyte -> SByteGraphType
            //   [17] Uri -> UriGraphType
            //
            // InputListTypes: 0
            
            """);
    }

    [Fact]
    public async Task DoNotMapClrTypeAttribute_PreventsClrTypeMapping()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            [DoNotMapClrType]
            public class ProductGraphType : ObjectGraphType<Product>
            {
                public ProductGraphType()
                {
                    Field(x => x.Name);
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ClrTypeMappingAttribute_OverridesInferredClrType()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class ProductDto
            {
                public string Title { get; set; }
            }

            [ClrTypeMapping(typeof(ProductDto))]
            public class ProductGraphType : ObjectGraphType<Product>
            {
                public ProductGraphType()
                {
                    Field(x => x.Name);
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 1
            //   [0] ProductDto -> ProductGraphType
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DoNotMapClrTypeAttribute_TakesPriorityOverClrTypeMappingAttribute()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class Product
            {
                public string Name { get; set; }
            }

            public class ProductDto
            {
                public string Title { get; set; }
            }

            [DoNotMapClrType]
            [ClrTypeMapping(typeof(ProductDto))]
            public class ProductGraphType : ObjectGraphType<Product>
            {
                public ProductGraphType()
                {
                    Field(x => x.Name);
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task GraphTypeWithObjectSourceType_DoesNotCreateMapping()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class ProductGraphType : ObjectGraphType<object>
            {
                public ProductGraphType()
                {
                    Field<StringGraphType>("name");
                }
            }

            [AotGraphType<ProductGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= SchemaTransformationReport.g.cs ============

            // Schema: MySchema
            //
            // QueryRootGraphType: (none)
            //
            // MutationRootGraphType: (none)
            //
            // SubscriptionRootGraphType: (none)
            //
            // DiscoveredGraphTypes: 1
            //   [0] ProductGraphType
            //
            // OutputClrTypeMappings: 0
            //
            // InputClrTypeMappings: 0
            //
            // InputListTypes: 0

            """);
    }
}
