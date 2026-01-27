using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.TypeSymbolTransformerReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Tests for TypeSymbolTransformer transformation logic for output types with method parameters.
/// These tests verify that the transformer correctly scans method parameters as input types.
/// Uses ReportingGenerator to isolate testing of TypeSymbolTransformer from the full pipeline.
/// </summary>
public partial class OutputTypeMethodParametersTests
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
                // Method with primitive parameters
                public ulong GetName(string name, byte quantity, decimal price) => 0UL;
                
                // Method with nullable parameters
                public ulong ProcessOptional(int? optionalInt, DateTime? optionalDate, string? optionalString) => 0UL;
                
                // Method with list parameters of nullable types
                public ulong HandleArrays(
                    int?[] optionalIntArray,
                    List<long?> nullableLongList,
                    List<Class1?> nullableClass1List,
                    Class2 class2Prop,
                    List<Class2?> nullableClass2List,
                    List<Class2> class2List) => 0UL;
                
                // Method with whitelisted collection type parameters
                public ulong ProcessCollections(
                    IEnumerable<string> iEnumerableStrings,
                    IList<int> iListInts,
                    List<bool> listBools,
                    ICollection<decimal> iCollectionDecimals,
                    IReadOnlyCollection<long> iReadOnlyCollectionLongs,
                    IReadOnlyList<short> iReadOnlyListShorts,
                    HashSet<byte> hashSetBytes,
                    ISet<double> iSetDoubles) => 0UL;
                
                // Method with array type parameters
                public ulong ProcessArrays(string[] stringArray, int[] intArray) => 0UL;
                
                // Method with non-whitelisted collection type parameters
                public ulong ProcessNonWhitelisted(
                    Queue<string> queueStrings,
                    Stack<int> stackInts,
                    LinkedList<bool> linkedListBools) => 0UL;
                
                // Method with nested array type parameters
                public ulong ProcessNested(
                    string[][] nestedStringArray,
                    List<short[]> listOfStringArrays,
                    IEnumerable<List<int>> enumerableOfListOfInts) => 0UL;
                
                // Multiple methods with same parameter types - should only appear once
                public ulong FirstMethod(int[] intArray) => 0UL;
                public ulong SecondMethod(int[] intArray) => 0UL;
                
                // List<int[]> contains int[] as nested - int[] should not be duplicated
                public ulong ListOfArrays(List<int[]> listOfIntArrays) => 0UL;
                
                // Multiple methods with List<string> - should only appear once
                public ulong FirstStringList(List<string> stringList) => 0UL;
                public ulong SecondStringList(List<string> stringList) => 0UL;
                
                // IEnumerable<List<int>> contains List<int> - both should appear once
                public ulong DirectListMethod(List<int> directListOfInts) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AllCollectionsOutput
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
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public ulong ProcessOrder(int? quantity, decimal? price, DateTime? releaseDate) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 3
            //   [0] int
            //   [1] decimal
            //   [2] DateTime
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task SkipsParameterAttributes()
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
                public ulong ProcessProduct(string name, [FromServices] int internalId, decimal price) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task MemberScan_OnlyMethods()
    {
        const string source =
            """
            using System;
            using GraphQL;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            [ScanMe(false)]
            [MemberScan(ScanMemberTypes.Methods)]
            public class CreateProductOutput
            {
                // Property should be skipped
                public string Name { get; set; }
                
                // Field should be skipped
                public Guid Identifier;
                
                // Only method parameters should be scanned
                public ulong ProcessData(decimal price, int quantity) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] decimal
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public int ComputedValue { get; }
                public decimal Price { get; set; }
                public Guid Identifier;
                public readonly sbyte? ReadOnlyField;
                
                // Method parameters should not be scanned
                public ulong GetInfo(int value) => 3UL;
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
                public ulong ProcessData(
                    string stringValue,
                    [InputType<IntGraphType>] int intValue,
                    [InputBaseType<ByteGraphType>] byte byteValue,
                    [BaseGraphType<LongGraphType>] long longValue,
                    [InputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>] DateTime[] dateTimeValue,
                    [Id] Guid idValue) => 0UL;
                
                // Duplicate types in another method
                public ulong ProcessMore(
                    [InputType<IntGraphType>] int intValue2,
                    [InputBaseType<ByteGraphType>] byte byteValue2,
                    [BaseGraphType<LongGraphType>] long longValue2,
                    [InputType<ListGraphType<NonNullGraphType<DateTimeGraphType>>>] DateTime[] dateTimeValue2,
                    [Id] Guid idValue2) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public ulong ProcessData(
                    string stringValue,
                    [InputType(typeof(IntGraphType))] int intValue,
                    [InputBaseType(typeof(ByteGraphType))] byte byteValue,
                    [BaseGraphType(typeof(LongGraphType))] long longValue,
                    [InputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))] DateTime[] dateTimeValue,
                    [Id] Guid idValue) => 0UL;
                
                // Duplicates
                public ulong ProcessMore(
                    [InputType(typeof(IntGraphType))] int intValue2,
                    [InputBaseType(typeof(ByteGraphType))] byte byteValue2,
                    [BaseGraphType(typeof(LongGraphType))] long longValue2,
                    [InputType(typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))] DateTime[] dateTimeValue2,
                    [InputType(typeof(IdGraphType))] Guid idValue2,
                    [InputType(graphType: typeof(SByteGraphType))] sbyte sByteValue) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public ulong ProcessData(
                    string stringValue,
                    [InputType(graphType: typeof(IntGraphType))] int intValue,
                    [InputBaseType(graphType: typeof(ByteGraphType))] byte byteValue,
                    [BaseGraphType(graphType: typeof(LongGraphType))] long longValue,
                    [InputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))] DateTime[] dateTimeValue,
                    [Id] Guid idValue) => 0UL;
                
                // Duplicates
                public ulong ProcessMore(
                    [InputType(graphType: typeof(IntGraphType))] int intValue2,
                    [InputBaseType(graphType: typeof(ByteGraphType))] byte byteValue2,
                    [BaseGraphType(graphType: typeof(LongGraphType))] long longValue2,
                    [InputType(graphType: typeof(ListGraphType<NonNullGraphType<DateTimeGraphType>>))] DateTime[] dateTimeValue2,
                    [Id] Guid idValue2) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 1
            //   [0] string
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public T ProcessValue(T value) => value;
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
                // Method with no parameters
                public ulong GetValue() => 42UL;
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
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public ulong CreateProduct(string name, decimal price) => 0UL;
            }

            [ScanMe(false)]
            public class CreateOrderOutput
            {
                public ulong CreateOrder(string orderNumber, int quantity) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateOrderOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0
            
            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] decimal
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
                public Task<ulong> GetNameAsync(Task<string> name, ValueTask<int> count) => Task.FromResult(0UL);
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: AsyncOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] string
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
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
            public record CreateProductOutput
            {
                public ulong CreateProduct(string name, decimal price, int quantity) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 3
            //   [0] string
            //   [1] decimal
            //   [2] int
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

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

            [ScanMe(false)]
            public class CreateProductOutput
            {
                public ulong CreateProduct(
                    string name,
                    [InputType(typeof(GraphQLClrInputTypeReference<long>))] int quantity,
                    [InputType(typeof(GraphQLClrInputTypeReference<long?>))] int quantity2,
                    [InputType<GraphQLClrInputTypeReference<decimal>>] decimal price,
                    [InputType<GraphQLClrInputTypeReference<Class1>>] decimal test1) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: CreateProductOutput
            //
            // DiscoveredInputClrTypes: 4
            //   [0] string
            //   [1] long
            //   [2] decimal
            //   [3] Class1
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task OnlyScansPublicMethodsFromDerivedClass()
    {
        const string source =
            """
            using System;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public class BaseOutput
            {
                private ulong PrivateBaseMethod(byte value) => 0UL;
                protected ulong ProtectedBaseMethod(short value) => 0UL;
                public ulong PublicBaseMethod(int value) => 0UL;
            }

            [ScanMe(false)]
            public class DerivedOutput : BaseOutput
            {
                private ulong PrivateDerivedMethod(long value) => 0UL;
                protected ulong ProtectedDerivedMethod(float value) => 0UL;
                public ulong PublicDerivedMethod(double value) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: DerivedOutput
            //
            // DiscoveredInputClrTypes: 2
            //   [0] double
            //   [1] int
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 0

            """);
    }

    [Fact]
    public async Task HandlesMultipleParametersInSingleMethod()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace Sample;

            [AttributeUsage(AttributeTargets.Class)]
            public class ScanMeAttribute : Attribute { public ScanMeAttribute(bool isInputType) { } }

            public class AddressInput { }
            public class ContactInput { }

            [ScanMe(false)]
            public class UserOutput
            {
                public ulong CreateUser(
                    string firstName,
                    string lastName,
                    int age,
                    decimal salary,
                    AddressInput address,
                    List<ContactInput> contacts,
                    string[] emails,
                    Dictionary<string, string> metadata) => 0UL;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= TypeScanReport.g.cs ============

            // Type: UserOutput
            //
            // DiscoveredInputClrTypes: 6
            //   [0] string
            //   [1] int
            //   [2] decimal
            //   [3] AddressInput
            //   [4] ContactInput
            //   [5] Dictionary<string, string>
            //
            // DiscoveredOutputClrTypes: 1
            //   [0] ulong
            //
            // DiscoveredGraphTypes: 0
            //
            // InputListTypes: 2
            //   [0] List<ContactInput>
            //   [1] string[]

            """);
    }
}
