using GraphQL.Types;
using System;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class ScalarsInputTest : QueryTestBase<SchemaForScalars>
    {
        [Fact]
        public void Scalars_Should_Return_As_Is()
        {
            var query = @"
mutation {
  create(input: {id1:""8dfab389-a6f7-431d-ab4e-aa693cc53edf"", id2:""8dfab389-a6f7-431d-ab4e-aa693cc53ede"", uint: 3147483647, uintArray: [3147483640], short: -21000, shortArray: [20000] ushort: 61000, ushortArray: [65000], ulong: 4000000000000, ulongArray: [1234567890123456789], byte: 50, byteArray: [1,2,3], sbyte: -60, sbyteArray: [-1,2,-3], dec: 39614081257132168796771975168, decArray: [1,39614081257132168796771975168,3] })
  {
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
    decArray
  }
  create_with_defaults(input: { })
  {
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
    decArray
  }
}
";
            var expected = @"{
  ""data"": {
    ""create"": {
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
      ""decArray"": [
        1,
        39614081257132168796771975168,
        3
      ]
    },
    ""create_with_defaults"": {
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
        public decimal[] decArray { get; set; }
    }

    public class ScalarsInput : InputObjectGraphType<ScalarsModel>
    {
        public ScalarsInput()
        {
            Name = "ScalarsInput";

            Field("id1", o => o.Id1, type: typeof(IdGraphType));
            Field("id2", o => o.Id2, type: typeof(GuidGraphType));
            Field("uint", o => o.uInt, type: typeof(UIntGraphType));
            Field("short", o => o.sHort, type: typeof(ShortGraphType));
            Field("ushort", o => o.uShort, type: typeof(UShortGraphType));
            Field("ulong", o => o.uLong, type: typeof(ULongGraphType));
            Field("byte", o => o.bYte, type: typeof(ByteGraphType));
            Field("sbyte", o => o.sByte, type: typeof(SByteGraphType));
            Field("dec", o => o.dec, type: typeof(DecimalGraphType));

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

            Field("id1", o => o.Id1, type: typeof(NonNullGraphType<IdGraphType>)).DefaultValue(new Guid("8dfab389-a6f7-431d-ab4e-aa693cc53edf"));
            Field("id2", o => o.Id2, type: typeof(NonNullGraphType<GuidGraphType>)).DefaultValue(new Guid("8dfab389-a6f7-431d-ab4e-aa693cc53ede"));
            Field("uint", o => o.uInt, type: typeof(NonNullGraphType<UIntGraphType>)).DefaultValue((uint)3147483647);
            Field("short", o => o.sHort, type: typeof(NonNullGraphType<ShortGraphType>)).DefaultValue((short)-21000);
            Field("ushort", o => o.uShort, type: typeof(NonNullGraphType<UShortGraphType>)).DefaultValue((ushort)61000);
            Field("ulong", o => o.uLong, type: typeof(NonNullGraphType<ULongGraphType>)).DefaultValue((ulong)4000000000000);
            Field("byte", o => o.bYte, type: typeof(NonNullGraphType<ByteGraphType>)).DefaultValue((byte)50);
            Field("sbyte", o => o.sByte, type: typeof(NonNullGraphType<SByteGraphType>)).DefaultValue((sbyte)-60);
            Field("dec", o => o.dec, type: typeof(NonNullGraphType<DecimalGraphType>)).DefaultValue(39614081257132168796771975168m);

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

            Field("id1", o => o.Id1, type: typeof(IdGraphType));
            Field("id2", o => o.Id2, type: typeof(GuidGraphType));
            Field("uint", o => o.uInt, type: typeof(UIntGraphType));
            Field("short", o => o.sHort, type: typeof(ShortGraphType));
            Field("ushort", o => o.uShort, type: typeof(UShortGraphType));
            Field("ulong", o => o.uLong, type: typeof(ULongGraphType));
            Field("byte", o => o.bYte, type: typeof(ByteGraphType));
            Field("sbyte", o => o.sByte, type: typeof(SByteGraphType));
            Field("dec", o => o.dec, type: typeof(DecimalGraphType));

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
                    return arg;
                });

            Field<ScalarsType>(
                "create_with_defaults",
                arguments: new QueryArguments(new QueryArgument<ScalarsInputWithDefaults> { Name = "input" }),
                resolve: ctx =>
                {
                    var arg = ctx.GetArgument<ScalarsModel>("input");
                    return arg;
                });
        }
    }
}
