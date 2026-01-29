using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.TypeSymbolTransformerReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators.Transformers;

/*
 * 
 * These tests rely on KnownSymbolsProvider working properly
 * 
 */

/// <summary>
/// Tests for TypeSymbolTransformer transformation logic.
/// These tests verify that the transformer correctly scans CLR input types and discovers dependencies.
/// Uses ReportingGenerator to isolate testing of TypeSymbolTransformer from the full pipeline.
/// </summary>
public partial class InputTypeSymbolTransformerTests
{
    [Fact]
    public async Task MasterList()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public class Class1 { }
            public class Class2
            {
                public sbyte Value { get; set; }
            }

            [ScanMe(true)]
            public class AllCollectionsInput
            {
                // Primitive properties
                public string Name { get; set; }
                public byte Quantity { get; set; }
                public decimal Price { get; set; }

                // Nullable properties
                public int? OptionalInt { get; set; }
                public DateTime? OptionalDate { get; set; }
                public string? OptionalString { get; set; }

                // Lists of nullable types
                public int?[] OptionalIntArray { get; set; }
                public List<long?> NullableLongList { get; set; }
                public List<Class1?> NullableClass1List { get; set; }
                public Class2 Class2Prop { get; set; } // test for deduplication
                public List<Class2?> NullableClass2List { get; set; }
                public List<Class2> Class2List { get; set; }

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

                // Multiple properties with int[] - should only appear once
                public int[] FirstIntArray { get; set; }
                public int[] SecondIntArray { get; set; }
                
                // List<int[]> contains int[] as nested - int[] should not be duplicated
                public List<int[]> ListOfIntArrays { get; set; }
                
                // Multiple List<string> - should only appear once
                public List<string> FirstStringList { get; set; }
                public List<string> SecondStringList { get; set; }
                
                // IEnumerable<List<int>> contains List<int> - both should appear once
                public List<int> DirectListOfInts { get; set; }

            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AllCollectionsInput
            //
            // DiscoveredInputClrTypes: 14
            //   [0] string
            //   [1] byte
            //   [2] decimal
            //   [3] int
            //   [4] DateTime
            //   [5] long
            //   [6] Class1
            //   [7] Class2
            //   [8] bool
            //   [9] short
            //   [10] double
            //   [11] Queue<string>
            //   [12] Stack<int>
            //   [13] LinkedList<bool>
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 21
            //   [0] int?[]
            //   [1] List<long?>
            //   [2] List<Class1?>
            //   [3] List<Class2?>
            //   [4] IEnumerable<string>
            //   [5] IList<int>
            //   [6] List<bool>
            //   [7] ICollection<decimal>
            //   [8] IReadOnlyCollection<long>
            //   [9] IReadOnlyList<short>
            //   [10] HashSet<byte>
            //   [11] ISet<double>
            //   [12] string[]
            //   [13] int[]
            //   [14] string[][]
            //   [15] List<short[]>
            //   [16] short[]
            //   [17] IEnumerable<List<int>>
            //   [18] List<int>
            //   [19] List<int[]>
            //   [20] List<string>
            //
            // SelectedMembers: 34
            //   [0] Property: string AllCollectionsInput.Name
            //   [1] Property: byte AllCollectionsInput.Quantity
            //   [2] Property: decimal AllCollectionsInput.Price
            //   [3] Property: int? AllCollectionsInput.OptionalInt
            //   [4] Property: DateTime? AllCollectionsInput.OptionalDate
            //   [5] Property: string? AllCollectionsInput.OptionalString
            //   [6] Property: int?[] AllCollectionsInput.OptionalIntArray
            //   [7] Property: List<long?> AllCollectionsInput.NullableLongList
            //   [8] Property: List<Class1?> AllCollectionsInput.NullableClass1List
            //   [9] Property: Class2 AllCollectionsInput.Class2Prop
            //   [10] Property: List<Class2?> AllCollectionsInput.NullableClass2List
            //   [11] Property: List<Class2> AllCollectionsInput.Class2List
            //   [12] Property: IEnumerable<string> AllCollectionsInput.IEnumerableStrings
            //   [13] Property: IList<int> AllCollectionsInput.IListInts
            //   [14] Property: List<bool> AllCollectionsInput.ListBools
            //   [15] Property: ICollection<decimal> AllCollectionsInput.ICollectionDecimals
            //   [16] Property: IReadOnlyCollection<long> AllCollectionsInput.IReadOnlyCollectionLongs
            //   [17] Property: IReadOnlyList<short> AllCollectionsInput.IReadOnlyListShorts
            //   [18] Property: HashSet<byte> AllCollectionsInput.HashSetBytes
            //   [19] Property: ISet<double> AllCollectionsInput.ISetDoubles
            //   [20] Property: string[] AllCollectionsInput.StringArray
            //   [21] Property: int[] AllCollectionsInput.IntArray
            //   [22] Property: Queue<string> AllCollectionsInput.QueueStrings
            //   [23] Property: Stack<int> AllCollectionsInput.StackInts
            //   [24] Property: LinkedList<bool> AllCollectionsInput.LinkedListBools
            //   [25] Property: string[][] AllCollectionsInput.NestedStringArray
            //   [26] Property: List<short[]> AllCollectionsInput.ListOfStringArrays
            //   [27] Property: IEnumerable<List<int>> AllCollectionsInput.EnumerableOfListOfInts
            //   [28] Property: int[] AllCollectionsInput.FirstIntArray
            //   [29] Property: int[] AllCollectionsInput.SecondIntArray
            //   [30] Property: List<int[]> AllCollectionsInput.ListOfIntArrays
            //   [31] Property: List<string> AllCollectionsInput.FirstStringList
            //   [32] Property: List<string> AllCollectionsInput.SecondStringList
            //   [33] Property: List<int> AllCollectionsInput.DirectListOfInts

            """);
    }

    [Fact]
    public async Task UnwrapsNullableValueTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
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
            // DiscoveredInputClrTypes: 3
            //   [0] int
            //   [1] decimal
            //   [2] DateTime
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 3
            //   [0] Property: int? CreateProductInput.Quantity
            //   [1] Property: decimal? CreateProductInput.Price
            //   [2] Property: DateTime? CreateProductInput.ReleaseDate

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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [Ignore]
                public int InternalId { get; set; }
                
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
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: decimal CreateProductInput.Price

            """);
    }

    [Fact]
    public async Task SkipsReadOnlyPropertiesAndFieldsForInputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                // Read-only properties should be skipped for input types
                public int ComputedValue { get; }
                
                public decimal Price { get; set; }

                // Fields should be skipped by default
                public Guid Identifier;

                // Methods should be skipped
                public byte GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: decimal CreateProductInput.Price

            """);
    }

    [Fact]
    public async Task MemberScan_All()
    {
        const string source =
            """
            using System;
            using GraphQL;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            [MemberScan(ScanMemberTypes.Fields | ScanMemberTypes.Properties | ScanMemberTypes.Methods)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                // Read-only properties should be skipped for input types
                public int ComputedValue { get; }
                
                public decimal Price { get; set; }

                public Guid Identifier;

                // Read-only fields should be skipped for input types
                public readonly sbyte? ReadOnlyField;

                // Methods should be skipped
                public short GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 3
            //   [0] Guid
            //   [1] string
            //   [2] decimal
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 3
            //   [0] Field: Guid CreateProductInput.Identifier
            //   [1] Property: string CreateProductInput.Name
            //   [2] Property: decimal CreateProductInput.Price

            """);
    }

    [Fact]
    public async Task MemberScan_None()
    {
        const string source =
            """
            using System;
            using GraphQL;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            [MemberScan(0)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                // Read-only properties should be skipped for input types
                public int ComputedValue { get; }
                
                public decimal Price { get; set; }

                public Guid Identifier;

                // Read-only fields should be skipped for input types
                public readonly sbyte? ReadOnlyField;

                // Methods should be skipped
                public short GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 0

            """);
    }

    [Fact]
    public async Task ReadsTypeAttributes()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string StringValue { get; set; }
                
                [InputType<IntGraphType>]
                public int IntValue { get; set; }
                
                [InputBaseType<ByteGraphType>]
                public byte ByteValue { get; set; }
                
                [BaseGraphType<LongGraphType>]
                public long LongValue { get; set; }
                
                [InputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [InputType<IntGraphType>]
                public int IntValue2 { get; set; }
                
                [InputBaseType<ByteGraphType>]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType<LongGraphType>]
                public long LongValue2 { get; set; }
                
                [InputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>]
                public DateTime[] DateTimeValue2 { get; set; }
            
                [Id]
                public Guid IdValue2 { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 5
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //
            // InputListTypes: 1
            //   [0] DateTime[]
            //
            // SelectedMembers: 11
            //   [0] Property: string CreateProductInput.StringValue
            //   [1] Property: int CreateProductInput.IntValue
            //   [2] Property: byte CreateProductInput.ByteValue
            //   [3] Property: long CreateProductInput.LongValue
            //   [4] Property: DateTime[] CreateProductInput.DateTimeValue
            //   [5] Property: Guid CreateProductInput.IdValue
            //   [6] Property: int CreateProductInput.IntValue2
            //   [7] Property: byte CreateProductInput.ByteValue2
            //   [8] Property: long CreateProductInput.LongValue2
            //   [9] Property: DateTime[] CreateProductInput.DateTimeValue2
            //   [10] Property: Guid CreateProductInput.IdValue2

            """);
    }

    [Fact]
    public async Task ReadsTypeAttributes_ConstructorSyntax()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string StringValue { get; set; }
                
                [InputType(typeof(IntGraphType))]
                public int IntValue { get; set; }
                
                [InputBaseType(typeof(ByteGraphType))]
                public byte ByteValue { get; set; }
                
                [BaseGraphType(typeof(LongGraphType))]
                public long LongValue { get; set; }
                
                [InputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [InputType(typeof(IntGraphType))]
                public int IntValue2 { get; set; }
                
                [InputBaseType(typeof(ByteGraphType))]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType(typeof(LongGraphType))]
                public long LongValue2 { get; set; }
                
                [InputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue2 { get; set; }
            
                [InputType(typeof(IdGraphType))]
                public Guid IdValue2 { get; set; }
            
                [InputType(graphType: typeof(SByteGraphType))]
                public sbyte SByteValue { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 6
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //   [5] SByteGraphType
            //
            // InputListTypes: 1
            //   [0] DateTime[]
            //
            // SelectedMembers: 12
            //   [0] Property: string CreateProductInput.StringValue
            //   [1] Property: int CreateProductInput.IntValue
            //   [2] Property: byte CreateProductInput.ByteValue
            //   [3] Property: long CreateProductInput.LongValue
            //   [4] Property: DateTime[] CreateProductInput.DateTimeValue
            //   [5] Property: Guid CreateProductInput.IdValue
            //   [6] Property: int CreateProductInput.IntValue2
            //   [7] Property: byte CreateProductInput.ByteValue2
            //   [8] Property: long CreateProductInput.LongValue2
            //   [9] Property: DateTime[] CreateProductInput.DateTimeValue2
            //   [10] Property: Guid CreateProductInput.IdValue2
            //   [11] Property: sbyte CreateProductInput.SByteValue

            """);
    }

    [Fact]
    public async Task ReadsTypeAttributes_NamedParameterSyntax()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string StringValue { get; set; }
                
                [InputType(graphType: typeof(IntGraphType))]
                public int IntValue { get; set; }
                
                [InputBaseType(graphType: typeof(ByteGraphType))]
                public byte ByteValue { get; set; }
                
                [BaseGraphType(graphType: typeof(LongGraphType))]
                public long LongValue { get; set; }
                
                [InputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [InputType(graphType: typeof(IntGraphType))]
                public int IntValue2 { get; set; }
                
                [InputBaseType(graphType: typeof(ByteGraphType))]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType(graphType: typeof(LongGraphType))]
                public long LongValue2 { get; set; }
                
                [InputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue2 { get; set; }
            
                [Id]
                public Guid IdValue2 { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 5
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //
            // InputListTypes: 1
            //   [0] DateTime[]
            //
            // SelectedMembers: 11
            //   [0] Property: string CreateProductInput.StringValue
            //   [1] Property: int CreateProductInput.IntValue
            //   [2] Property: byte CreateProductInput.ByteValue
            //   [3] Property: long CreateProductInput.LongValue
            //   [4] Property: DateTime[] CreateProductInput.DateTimeValue
            //   [5] Property: Guid CreateProductInput.IdValue
            //   [6] Property: int CreateProductInput.IntValue2
            //   [7] Property: byte CreateProductInput.ByteValue2
            //   [8] Property: long CreateProductInput.LongValue2
            //   [9] Property: DateTime[] CreateProductInput.DateTimeValue2
            //   [10] Property: Guid CreateProductInput.IdValue2

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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
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
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 0

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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            [ScanMe(true)]
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
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: string CreateOrderInput.OrderNumber
            //   [1] Property: int CreateOrderInput.Quantity
            
            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: decimal CreateProductInput.Price

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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
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
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: Task<string> AsyncInput.Name
            //   [1] Property: ValueTask<int> AsyncInput.Count

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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public record CreateProductInput(string Name, decimal Price, int Quantity);
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 3
            //   [0] string
            //   [1] decimal
            //   [2] int
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 3
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: decimal CreateProductInput.Price
            //   [2] Property: int CreateProductInput.Quantity

            """);
    }

    [Fact]
    public async Task GraphQLClrInputTypeReference_AddsToClrTypes()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public class Class1 { }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                [InputType(typeof(GraphQLClrInputTypeReference<long>))]
                public int Quantity { get; set; }
                
                [InputType(typeof(GraphQLClrInputTypeReference<long?>))]
                public int Quantity2 { get; set; }
                
                [InputType<GraphQLClrInputTypeReference<decimal>>]
                public decimal Price { get; set; }

                [InputType<GraphQLClrInputTypeReference<Class1>>]
                public decimal Test1 { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 4
            //   [0] string
            //   [1] long
            //   [2] decimal
            //   [3] Class1
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 5
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: int CreateProductInput.Quantity
            //   [2] Property: int CreateProductInput.Quantity2
            //   [3] Property: decimal CreateProductInput.Price
            //   [4] Property: decimal CreateProductInput.Test1

            """);
    }

    [Fact]
    public async Task OnlyScansPublicPropertiesFromDerivedClass()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public class BaseInput
            {
                private byte PrivateBaseProp { get; set; }
                protected short ProtectedBaseProp { get; set; }
                public int PublicBaseProp { get; set; }
            }

            [ScanMe(true)]
            public class DerivedInput : BaseInput
            {
                private long PrivateDerivedProp { get; set; }
                protected float ProtectedDerivedProp { get; set; }
                public double PublicDerivedProp { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: DerivedInput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] double
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: double DerivedInput.PublicDerivedProp
            //   [1] Property: int BaseInput.PublicBaseProp

            """);
    }

    [Fact]
    public async Task SkipsStaticPropertiesForInputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            public class CreateProductInput
            {
                public string Name { get; set; }
                
                // Static properties should be skipped for input types
                public static int GlobalCounter { get; set; }
                public static decimal DefaultPrice { get; set; }
                
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
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Property: string CreateProductInput.Name
            //   [1] Property: int CreateProductInput.Quantity

            """);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IncludesReadOnlyFieldWithMatchingConstructorParameter(bool isClass)
    {
        string source =
            $$"""
            using System;
            using GraphQL;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(true)]
            [MemberScan(ScanMemberTypes.Fields | ScanMemberTypes.Properties)]
            public {{(isClass ? "class" : "struct")}} CreateProductInput
            {
                // Readonly property with matching constructor parameter should be included
                public string Name { get; }
                
                // Readonly field with matching constructor parameter should be included
                public readonly int Quantity;

                public CreateProductInput(string name, int quantity)
                {
                    Name = name;
                    Quantity = quantity;
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductInput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] int
            //   [1] string
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            //
            // SelectedMembers: 2
            //   [0] Field: int CreateProductInput.Quantity
            //   [1] Property: string CreateProductInput.Name

            """);
    }
}
