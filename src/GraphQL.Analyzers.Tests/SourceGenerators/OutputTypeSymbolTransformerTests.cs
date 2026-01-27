using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.TypeSymbolTransformerReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for TypeSymbolTransformer transformation logic.
/// These tests verify that the transformer correctly scans CLR output types and discovers dependencies.
/// Uses ReportingGenerator to isolate testing of TypeSymbolTransformer from the full pipeline.
/// </summary>
public partial class OutputTypeSymbolTransformerTests
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

            [ScanMe(false)]
            public class AllCollectionsOutput
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

                // Methods (without arguments) should be scanned by default for output types
                public Guid GetIdentifier() => Guid.NewGuid();
                public float CalculateDiscount() => 0.1f;

            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AllCollectionsOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 16
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
            //   [14] Guid
            //   [15] float
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class CreateProductOutput
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

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 3
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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class CreateProductOutput
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

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task SkipsWriteOnlyPropertiesAndFieldsForOutputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                
                // Write-only properties should be skipped for output types
                private int _writeOnlyValue;
                public int WriteOnlyValue { set => _writeOnlyValue = value; }
                
                public decimal Price { get; set; }

                // Fields should be skipped by default
                public Guid Identifier;

                // Methods should be scanned by default for output types (without arguments)
                public byte GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 3
            //   [0] string
            //   [1] decimal
            //   [2] byte
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            [MemberScan(ScanMemberTypes.Fields | ScanMemberTypes.Properties | ScanMemberTypes.Methods)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                
                // Read-only properties should be included for output types
                public int ComputedValue { get; }
                
                public decimal Price { get; set; }

                public Guid Identifier;

                // Read-only fields should be included for output types
                public readonly sbyte? ReadOnlyField;

                // Methods should be scanned (without arguments)
                public short GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 6
            //   [0] Guid
            //   [1] sbyte
            //   [2] string
            //   [3] int
            //   [4] decimal
            //   [5] short
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            [MemberScan(0)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                
                // Read-only properties should be included for output types
                public int ComputedValue { get; }
                
                public decimal Price { get; set; }

                public Guid Identifier;

                // Read-only fields should be included for output types
                public readonly sbyte? ReadOnlyField;

                // Methods should be scanned
                public short GetInfo() => 3;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string StringValue { get; set; }
                
                [OutputType<IntGraphType>]
                public int IntValue { get; set; }
                
                [OutputBaseType<ByteGraphType>]
                public byte ByteValue { get; set; }
                
                [BaseGraphType<LongGraphType>]
                public long LongValue { get; set; }
                
                [OutputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [OutputType<IntGraphType>]
                public int IntValue2 { get; set; }
                
                [OutputBaseType<ByteGraphType>]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType<LongGraphType>]
                public long LongValue2 { get; set; }
                
                [OutputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>]
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

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 5
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string StringValue { get; set; }
                
                [OutputType(typeof(IntGraphType))]
                public int IntValue { get; set; }
                
                [OutputBaseType(typeof(ByteGraphType))]
                public byte ByteValue { get; set; }
                
                [BaseGraphType(typeof(LongGraphType))]
                public long LongValue { get; set; }
                
                [OutputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [OutputType(typeof(IntGraphType))]
                public int IntValue2 { get; set; }
                
                [OutputBaseType(typeof(ByteGraphType))]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType(typeof(LongGraphType))]
                public long LongValue2 { get; set; }
                
                [OutputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue2 { get; set; }
            
                [OutputType(typeof(IdGraphType))]
                public Guid IdValue2 { get; set; }
            
                [OutputType(graphType: typeof(SByteGraphType))]
                public sbyte SByteValue { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 6
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //   [5] SByteGraphType
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string StringValue { get; set; }
                
                [OutputType(graphType: typeof(IntGraphType))]
                public int IntValue { get; set; }
                
                [OutputBaseType(graphType: typeof(ByteGraphType))]
                public byte ByteValue { get; set; }
                
                [BaseGraphType(graphType: typeof(LongGraphType))]
                public long LongValue { get; set; }
                
                [OutputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
                public DateTime[] DateTimeValue { get; set; }

                [Id]
                public Guid IdValue { get; set; }

                // duplicates
                [OutputType(graphType: typeof(IntGraphType))]
                public int IntValue2 { get; set; }
                
                [OutputBaseType(graphType: typeof(ByteGraphType))]
                public byte ByteValue2 { get; set; }
                
                [BaseGraphType(graphType: typeof(LongGraphType))]
                public long LongValue2 { get; set; }
                
                [OutputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))]
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

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] string
            //
            // DiscoveredGraphTypes: 5
            //   [0] IntGraphType
            //   [1] ByteGraphType
            //   [2] LongGraphType
            //   [3] DateTimeGraphType
            //   [4] IdGraphType
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class GenericOutput<T>
            {
                public T Value { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: GenericOutput<T>
            // Result: null (cannot be scanned)
            //

            """);
    }

    [Fact]
    public async Task HandlesEmptyOutputType()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class EmptyOutput
            {
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: EmptyOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 0
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ScansMultipleOutputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            [ScanMe(false)]
            public class CreateOrderOutput
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

            // Type: CreateOrderOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            
            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 2
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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class AsyncOutput
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

            // Type: AsyncOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 2
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
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public record CreateProductOutput(string Name, decimal Price, int Quantity);
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 3
            //   [0] string
            //   [1] decimal
            //   [2] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task GraphQLClrOutputTypeReference_AddsToClrTypes()
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

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                
                [OutputType(typeof(GraphQLClrOutputTypeReference<long>))]
                public int Quantity { get; set; }
                
                [OutputType(typeof(GraphQLClrOutputTypeReference<long?>))]
                public int Quantity2 { get; set; }
                
                [OutputType<GraphQLClrOutputTypeReference<decimal>>]
                public decimal Price { get; set; }

                [OutputType<GraphQLClrOutputTypeReference<Class1>>]
                public decimal Test1 { get; set; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 4
            //   [0] string
            //   [1] long
            //   [2] decimal
            //   [3] Class1
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            public class BaseOutput
            {
                private byte PrivateBaseProp { get; set; }
                protected short ProtectedBaseProp { get; set; }
                public int PublicBaseProp { get; set; }
            }

            [ScanMe(false)]
            public class DerivedOutput : BaseOutput
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

            // Type: DerivedOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 2
            //   [0] double
            //   [1] int
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task ScansAllImplementedInterfacesForOutputType()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Interface)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public interface IBaseInterface1
            {
                string Property1 { get; }
                int Property2 { get; }
            }

            public interface IBaseInterface2
            {
                decimal Property3 { get; }
                byte Property4 { get; }
            }

            [ScanMe(false)]
            public interface IMainInterface : IBaseInterface1, IBaseInterface2
            {
                double Property5 { get; }
                long Property6 { get; }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: IMainInterface
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 6
            //   [0] double
            //   [1] long
            //   [2] string
            //   [3] int
            //   [4] decimal
            //   [5] byte
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task IncludesStaticPropertiesForOutputTypes()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public string Name { get; set; }
                
                // Static properties should be included for output types
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

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 0
            //
            // DiscoveredOutputClrTypes: 3
            //   [0] string
            //   [1] int
            //   [2] decimal
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }
}
