using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class ScalarsInputTest : QueryTestBase<SchemaForScalars>
{
    [Fact]
    public void Scalars_Should_Return_As_Is()
    {
        var query = @"
mutation {
  create(input: {float1: 1.3, floatFromInt: 1, id1:""8dfab389-a6f7-431d-ab4e-aa693cc53edf"", id2:""8dfab389-a6f7-431d-ab4e-aa693cc53ede"", uint: 3147483647, uintArray: [3147483640], short: -21000, shortArray: [20000] ushort: 61000, ushortArray: [65000], ulong: 4000000000000, ulongArray: [1234567890123456789], byte: 50, byteArray: [1,2,3], sbyte: -60, sbyteArray: [-1,2,-3], dec: 39614081257132168796771975168, decZero: 12.10, decArray: [1,39614081257132168796771975168,3] })
  {
    float1
    floatFromInt

    id1
    id2

    uint
    uintArray

    short
    shortArray

    ushort
    ushortArray

    ulong
    ulongArray

    byte
    byteArray

    sbyte
    sbyteArray

    dec
    decZero
    decArray
  }
  create_with_defaults(input: { })
  {
    float1
    floatFromInt

    id1
    id2

    uint
    uintArray

    short
    shortArray

    ushort
    ushortArray

    ulong
    ulongArray

    byte
    byteArray

    sbyte
    sbyteArray

    dec
    decZero
    decArray
  }
}
";
        var expected = @"{
  ""data"": {
    ""create"": {
      ""float1"": 1.2999999523162842,
      ""floatFromInt"": 1,
      ""id1"": ""8dfab389-a6f7-431d-ab4e-aa693cc53edf"",
      ""id2"": ""8dfab389-a6f7-431d-ab4e-aa693cc53ede"",
      ""uint"": 3147483647,
      ""uintArray"": [
        3147483640
      ],
      ""short"": -21000,
      ""shortArray"": [
        20000
      ],
      ""ushort"": 61000,
      ""ushortArray"": [
        65000
      ],
      ""ulong"": 4000000000000,
      ""ulongArray"": [
        1234567890123456789
      ],
      ""byte"": 50,
      ""byteArray"": [
        1,
        2,
        3
      ],
      ""sbyte"": -60,
      ""sbyteArray"": [
        -1,
        2,
        -3
      ],
      ""dec"": 39614081257132168796771975168,
      ""decZero"": 12.10,
      ""decArray"": [
        1,
        39614081257132168796771975168,
        3
      ]
    },
    ""create_with_defaults"": {
      ""float1"": 1.2999999523162842,
      ""floatFromInt"": 1,
      ""id1"": ""8dfab389-a6f7-431d-ab4e-aa693cc53edf"",
      ""id2"": ""8dfab389-a6f7-431d-ab4e-aa693cc53ede"",
      ""uint"": 3147483647,
      ""uintArray"": [
        3147483640
      ],
      ""short"": -21000,
      ""shortArray"": [
        20000
      ],
      ""ushort"": 61000,
      ""ushortArray"": [
        65000
      ],
      ""ulong"": 4000000000000,
      ""ulongArray"": [
        1234567890123456789
      ],
      ""byte"": 50,
      ""byteArray"": [
        1,
        2,
        3
      ],
      ""sbyte"": -60,
      ""sbyteArray"": [
        -1,
        2,
        -3
      ],
      ""dec"": 39614081257132168796771975168,
      ""decZero"": 12.10,
      ""decArray"": [
        1,
        39614081257132168796771975168,
        3
      ]
    }
  }
}";
        AssertQuery(query, expected, null, null);
    }

    [Fact]
    public void Should_Accept_Int_As_Int_In_Literal()
    {
        var query = @"mutation { int(number: 100) }";
        var expected = @"{ ""int"": 100 }";
        AssertQuerySuccess(query, expected, null, null);
    }

    [Fact]
    public void Should_Accept_Long_As_Long_In_Literal()
    {
        var query = @"mutation { long(number: 100) }";
        var expected = @"{ ""long"": 100 }";
        AssertQuerySuccess(query, expected, null, null);
    }

    [Fact]
    public void Should_Not_Accept_String_As_Int_In_Literal()
    {
        var query = @"mutation { int(number: ""100"") }";
        string expected = null;
        var result = AssertQueryWithErrors(query, expected, expectedErrorCount: 1, executed: false);
        result.Errors[0].Message.ShouldBe("Argument 'number' has invalid value. Expected type 'Int', found \"100\".");
    }

    [Fact]
    public void Should_Not_Accept_String_As_Int_In_Variable()
    {
        var query = @"mutation AAA($val : Int!) { int(number: $val) }";
        var expected = @"{ ""int"": 100 }";
        var result = AssertQueryWithErrors(query, expected, variables: @"{ ""val"": ""100"" }".ToInputs(), expectedErrorCount: 1, executed: false);
        result.Errors[0].Message.ShouldBe(@"Variable '$val' is invalid. Unable to convert '100' to 'Int'");
    }

    [Fact]
    public void Should_Not_Accept_String_As_Long_In_Literal()
    {
        var query = @"mutation { long(number: ""100"") }";
        string expected = null;
        var result = AssertQueryWithErrors(query, expected, expectedErrorCount: 1, executed: false);
        result.Errors[0].Message.ShouldBe("Argument 'number' has invalid value. Expected type 'Long', found \"100\".");
    }

    [Fact]
    public void Should_Not_Accept_String_As_Long_In_Variable()
    {
        var query = @"mutation AAA($val : Long!) { long(number: $val) }";
        var expected = @"{ ""long"": 100 }";
        var result = AssertQueryWithErrors(query, expected, variables: @"{ ""val"": ""100"" }".ToInputs(), expectedErrorCount: 1, executed: false);
        result.Errors[0].Message.ShouldBe(@"Variable '$val' is invalid. Unable to convert '100' to 'Long'");
    }
}

public class SchemaForScalars : Schema
{
    public SchemaForScalars()
    {
        Mutation = new ScalarsMutation();
    }
}

public class ScalarsModel
{
    public float Float1 { get; set; }

    public float FloatFromInt { get; set; }

    public Guid Id1 { get; set; }

    public Guid Id2 { get; set; }

    public uint uInt { get; set; }
    public uint[] uintArray { get; set; }

    public short sHort { get; set; }
    public short[] shortArray { get; set; }

    public ushort uShort { get; set; }
    public ushort[] ushortArray { get; set; }

    public ulong uLong { get; set; }
    public ulong[] ulongArray { get; set; }

    public byte bYte { get; set; }
    public byte[] byteArray { get; set; }

    public sbyte sByte { get; set; }
    public sbyte[] sbyteArray { get; set; }

    public decimal dec { get; set; }
    public decimal decZero { get; set; }
    public decimal[] decArray { get; set; }
}

public class ScalarsInput : InputObjectGraphType<ScalarsModel>
{
    public ScalarsInput()
    {
        Name = "ScalarsInput";

        Field("float1", o => o.Float1, type: typeof(FloatGraphType));
        Field("floatFromInt", o => o.FloatFromInt, type: typeof(FloatGraphType));
        Field("id1", o => o.Id1, type: typeof(IdGraphType));
        Field("id2", o => o.Id2, type: typeof(GuidGraphType));
        Field("uint", o => o.uInt, type: typeof(UIntGraphType));
        Field("short", o => o.sHort, type: typeof(ShortGraphType));
        Field("ushort", o => o.uShort, type: typeof(UShortGraphType));
        Field("ulong", o => o.uLong, type: typeof(ULongGraphType));
        Field("byte", o => o.bYte, type: typeof(ByteGraphType));
        Field("sbyte", o => o.sByte, type: typeof(SByteGraphType));
        Field("dec", o => o.dec, type: typeof(DecimalGraphType));
        Field("decZero", o => o.decZero, type: typeof(DecimalGraphType));

        Field(o => o.byteArray);
        Field(o => o.sbyteArray);
        Field(o => o.ulongArray);
        Field(o => o.uintArray);
        Field(o => o.shortArray);
        Field(o => o.ushortArray);
        Field(o => o.decArray);
    }
}

public class ScalarsInputWithDefaults : InputObjectGraphType<ScalarsModel>
{
    public ScalarsInputWithDefaults()
    {
        Name = "ScalarsInputWithDefaults";

        Field("float1", o => o.Float1, type: typeof(NonNullGraphType<FloatGraphType>)).DefaultValue(1.3f);
        Field("floatFromInt", o => o.FloatFromInt, type: typeof(NonNullGraphType<FloatGraphType>)).DefaultValue(1);
        Field("id1", o => o.Id1, type: typeof(NonNullGraphType<IdGraphType>)).DefaultValue(new Guid("8dfab389-a6f7-431d-ab4e-aa693cc53edf"));
        Field("id2", o => o.Id2, type: typeof(NonNullGraphType<GuidGraphType>)).DefaultValue(new Guid("8dfab389-a6f7-431d-ab4e-aa693cc53ede"));
        Field("uint", o => o.uInt, type: typeof(NonNullGraphType<UIntGraphType>)).DefaultValue((uint)3147483647);
        Field("short", o => o.sHort, type: typeof(NonNullGraphType<ShortGraphType>)).DefaultValue((short)-21000);
        Field("ushort", o => o.uShort, type: typeof(NonNullGraphType<UShortGraphType>)).DefaultValue((ushort)61000);
        Field("ulong", o => o.uLong, type: typeof(NonNullGraphType<ULongGraphType>)).DefaultValue((ulong)4000000000000);
        Field("byte", o => o.bYte, type: typeof(NonNullGraphType<ByteGraphType>)).DefaultValue((byte)50);
        Field("sbyte", o => o.sByte, type: typeof(NonNullGraphType<SByteGraphType>)).DefaultValue((sbyte)-60);
        Field("dec", o => o.dec, type: typeof(NonNullGraphType<DecimalGraphType>)).DefaultValue(39614081257132168796771975168m);
        Field("decZero", o => o.decZero, type: typeof(NonNullGraphType<DecimalGraphType>)).DefaultValue(12.10m);

        Field(o => o.byteArray, nullable: false).DefaultValue(new byte[] { 1, 2, 3 });
        Field(o => o.sbyteArray, nullable: false).DefaultValue(new sbyte[] { -1, 2, -3 });
        Field(o => o.ulongArray, nullable: false).DefaultValue(new ulong[] { 1234567890123456789 });
        Field(o => o.uintArray, nullable: false).DefaultValue(new uint[] { 3147483640 });
        Field(o => o.shortArray, nullable: false).DefaultValue(new short[] { 20000 });
        Field(o => o.ushortArray, nullable: false).DefaultValue(new ushort[] { 65000 });
        Field(o => o.decArray, nullable: false).DefaultValue(new decimal[] { 1, 39614081257132168796771975168m, 3 });
    }
}

public class ScalarsType : ObjectGraphType<ScalarsModel>
{
    public ScalarsType()
    {
        Name = "ScalarsType";

        Field("float1", o => o.Float1, type: typeof(FloatGraphType));
        Field("floatFromInt", o => o.FloatFromInt, type: typeof(FloatGraphType));
        Field("id1", o => o.Id1, type: typeof(IdGraphType));
        Field("id2", o => o.Id2, type: typeof(GuidGraphType));
        Field("uint", o => o.uInt, type: typeof(UIntGraphType));
        Field("short", o => o.sHort, type: typeof(ShortGraphType));
        Field("ushort", o => o.uShort, type: typeof(UShortGraphType));
        Field("ulong", o => o.uLong, type: typeof(ULongGraphType));
        Field("byte", o => o.bYte, type: typeof(ByteGraphType));
        Field("sbyte", o => o.sByte, type: typeof(SByteGraphType));
        Field("dec", o => o.dec, type: typeof(DecimalGraphType));
        Field("decZero", o => o.decZero, type: typeof(DecimalGraphType));

        Field(o => o.byteArray);
        Field(o => o.sbyteArray);
        Field(o => o.ulongArray);
        Field(o => o.uintArray);
        Field(o => o.shortArray);
        Field(o => o.ushortArray);
        Field(o => o.decArray);
    }
}

public class ScalarsMutation : ObjectGraphType
{
    public ScalarsMutation()
    {
        Name = "ScalarsMutation";

        Field<ScalarsType>(
            "create",
            arguments: new QueryArguments(new QueryArgument<ScalarsInput> { Name = "input" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<ScalarsModel>("input");
                arg.decZero.ShouldBe(12.10m);
                return arg;
            });

        Field<ScalarsType>(
            "create_with_defaults",
            arguments: new QueryArguments(new QueryArgument<ScalarsInputWithDefaults> { Name = "input" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<ScalarsModel>("input");
                arg.decZero.ShouldBe(12.10m);
                return arg;
            });

        Field<LongGraphType>(
            "long",
            arguments: new QueryArguments(new QueryArgument<LongGraphType> { Name = "number" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<long>("number");
                arg.ShouldBe(100L);
                return arg;
            });

        Field<IntGraphType>(
            "int",
            arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "number" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<int>("number");
                arg.ShouldBe(100);
                return arg;
            });
    }
}
