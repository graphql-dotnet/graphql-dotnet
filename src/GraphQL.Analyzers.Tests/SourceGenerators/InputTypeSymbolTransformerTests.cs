using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.InputTypeSymbolTransformerTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for TypeSymbolTransformer transformation logic.
/// These tests verify that the transformer correctly scans CLR input types and discovers dependencies.
/// Uses ReportingGenerator to isolate testing of TypeSymbolTransformer from the full pipeline.
/// </summary>
public partial class InputTypeSymbolTransformerTests
{
    [Fact]
    public async Task ScansSimpleInputTypeWithPrimitiveProperties()
    {
        const string source =
            """
            using System;

            namespace Sample;

            // Marker attribute for test purposes
            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                public int Quantity { get; set; }
                public decimal Price { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 3
            //   [0] string
            //   [1] int
            //   [2] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ScansInputTypeWithNestedType()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            [ScanMe]
            public class CreatePersonInput
            {
                public string Name { get; set; }
                public Address Address { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreatePersonInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] Address
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task DetectsListTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateOrderInput
            {
                public string OrderNumber { get; set; }
                public List<string> Tags { get; set; }
                public string[] Categories { get; set; }
                public IEnumerable<int> Quantities { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateOrderInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 3
            //   [0] List<string>
            //   [1] string[]
            //   [2] IEnumerable<int>

            """);
    }

    [Fact]
    public async Task UnwrapsNullableTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public int? Quantity { get; set; }
                public decimal? Price { get; set; }
                public DateTime? ReleaseDate { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 3
            //   [0] int
            //   [1] decimal
            //   [2] DateTime
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task SkipsIgnoredProperties()
    {
        const string source =
            """
            using System;
            using GraphQL;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [Ignore]
                public string InternalId { get; set; }
                
                public decimal Price { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task SkipsReadOnlyPropertiesForInputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                // Read-only properties should be skipped for input types
                public string ComputedValue { get; }
                
                public decimal Price { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesPropertyWithInputTypeAttribute()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [InputType<StringGraphType>]
                public string Description { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 1
            //   [0] StringGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesPropertyWithInputBaseTypeAttribute()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [InputBaseType<IdGraphType>]
                public string Id { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 1
            //   [0] IdGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesPropertyWithBaseGraphTypeAttribute()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [BaseGraphType<IntGraphType>]
                public int Quantity { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 1
            //   [0] IntGraphType
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesNestedListTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateMatrixInput
            {
                public List<List<int>> Matrix { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateMatrixInput
            //
            // DiscoveredClrTypes: 1
            //   [0] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 2
            //   [0] List<List<int>>
            //   [1] List<int>

            """);
    }

    [Fact]
    public async Task DetectsVariousCollectionTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CollectionsInput
            {
                public IList<string> Names { get; set; }
                public IReadOnlyList<int> Ids { get; set; }
                public ICollection<decimal> Prices { get; set; }
                public IReadOnlyCollection<bool> Flags { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CollectionsInput
            //
            // DiscoveredClrTypes: 4
            //   [0] string
            //   [1] int
            //   [2] decimal
            //   [3] bool
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 4
            //   [0] IList<string>
            //   [1] IReadOnlyList<int>
            //   [2] ICollection<decimal>
            //   [3] IReadOnlyCollection<bool>

            """);
    }

    [Fact]
    public async Task DetectsHashSetAndISetTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class SetCollectionsInput
            {
                public HashSet<string> Tags { get; set; }
                public ISet<int> Numbers { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: SetCollectionsInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 2
            //   [0] HashSet<string>
            //   [1] ISet<int>

            """);
    }

    [Fact]
    public async Task DetectsAllWhitelistedCollectionTypesAndArraysButNotOthers()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class AllCollectionsInput
            {
                // Whitelisted collection types
                public IEnumerable<string> IEnumerableStrings { get; set; }
                public IList<int> IListInts { get; set; }
                public List<bool> ListBools { get; set; }
                public ICollection<decimal> ICollectionDecimals { get; set; }
                public IReadOnlyCollection<long> IReadOnlyCollectionLongs { get; set; }
                public IReadOnlyList<short> IReadOnlyListShorts { get; set; }
                public HashSet<byte> HashSetBytes { get; set; }
                public ISet<double> ISetDoubles { get; set; }
                
                // Array types
                public string[] StringArray { get; set; }
                public int[] IntArray { get; set; }
                
                // Non-whitelisted collection types (should NOT be detected as list types)
                public Queue<string> QueueStrings { get; set; }
                public Stack<int> StackInts { get; set; }
                public LinkedList<bool> LinkedListBools { get; set; }

                // Nested array types
                public string[][] NestedStringArray { get; set; }
                public List<short[]> ListOfStringArrays { get; set; }
                public IEnumerable<List<int>> EnumerableOfListOfInts { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AllCollectionsInput
            //
            // DiscoveredClrTypes: 11
            //   [0] string
            //   [1] int
            //   [2] bool
            //   [3] decimal
            //   [4] long
            //   [5] short
            //   [6] byte
            //   [7] double
            //   [8] Queue<string>
            //   [9] Stack<int>
            //   [10] LinkedList<bool>
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 15
            //   [0] IEnumerable<string>
            //   [1] IList<int>
            //   [2] List<bool>
            //   [3] ICollection<decimal>
            //   [4] IReadOnlyCollection<long>
            //   [5] IReadOnlyList<short>
            //   [6] HashSet<byte>
            //   [7] ISet<double>
            //   [8] string[]
            //   [9] int[]
            //   [10] string[][]
            //   [11] List<short[]>
            //   [12] short[]
            //   [13] IEnumerable<List<int>>
            //   [14] List<int>

            """);
    }

    [Fact]
    public async Task DeduplicatesListTypesIncludingNestedTypes()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class DeduplicationInput
            {
                // Multiple properties with int[] - should only appear once
                public int[] FirstIntArray { get; set; }
                public int[] SecondIntArray { get; set; }
                
                // List<int[]> contains int[] as nested - int[] should not be duplicated
                public List<int[]> ListOfIntArrays { get; set; }
                
                // Multiple List<string> - should only appear once
                public List<string> FirstStringList { get; set; }
                public List<string> SecondStringList { get; set; }
                
                // IEnumerable<List<int>> contains List<int> - both should appear once
                public IEnumerable<List<int>> EnumerableOfListOfInts { get; set; }
                public List<int> DirectListOfInts { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: DeduplicationInput
            //
            // DiscoveredClrTypes: 2
            //   [0] int
            //   [1] string
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 5
            //   [0] int[]
            //   [1] List<int[]>
            //   [2] List<string>
            //   [3] IEnumerable<List<int>>
            //   [4] List<int>

            """);
    }

    [Fact]
    public async Task ReturnsNullForOpenGenericTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class GenericInput<T>
            {
                public T Value { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: GenericInput<T>
            // Result: null (cannot be scanned)
            //

            """);
    }

    [Fact]
    public async Task HandlesEmptyInputType()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class EmptyInput
            {
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: EmptyInput
            //
            // DiscoveredClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ScansMultipleInputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class CreateProductInput
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            [ScanMe]
            public class CreateOrderInput
            {
                public string OrderNumber { get; set; }
                public int Quantity { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateOrderInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            
            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task UnwrapsTaskTypes()
    {
        const string source =
            """
            using System;
            using System.Threading.Tasks;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public class AsyncInput
            {
                public Task<string> Name { get; set; }
                public ValueTask<int> Count { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AsyncInput
            //
            // DiscoveredClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesRecordTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { }

            [ScanMe]
            public record CreateProductInput(string Name, decimal Price, int Quantity);
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredClrTypes: 3
            //   [0] string
            //   [1] decimal
            //   [2] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }
}
