using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;
using Shouldly;
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
  create
  (
    input: {id1:""8dfab389-a6f7-431d-ab4e-aa693cc53edf"", id2:""8dfab389-a6f7-431d-ab4e-aa693cc53ede"", uint: 3147483647, uintArray: [3147483640], short: -21000, shortArray: [20000] ushort: 61000, ushortArray: [65000], ulong: 4000000000000, ulongArray: [1234567890123456789], byte: 50, byteArray: [1,2,3], sbyte: -60, sbyteArray: [-1,2,-3]},
    binary: ""R3JhcGhRTCE=""
  )
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
  }
}
";
            var expected = @"{
  ""create"": {
    ""id1"": ""8dfab389-a6f7-431d-ab4e-aa693cc53edf"",
    ""id2"": ""8dfab389-a6f7-431d-ab4e-aa693cc53ede"",
    ""uint"": 3147483647,
    ""uintArray"": [3147483640],
    ""short"": -21000,
    ""shortArray"": [20000],
    ""ushort"": 61000,
    ""ushortArray"": [65000],
    ""ulong"": 4000000000000,
    ""ulongArray"": [1234567890123456789],
    ""byte"": 50,
    ""byteArray"": [1,2,3],
    ""sbyte"": -60,
    ""sbyteArray"": [-1,2,-3]
  }
}";
            AssertQuerySuccess(query, expected, null);
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

            Field(o => o.byteArray);
            Field(o => o.sbyteArray);
            Field(o => o.ulongArray);
            Field(o => o.uintArray);
            Field(o => o.shortArray);
            Field(o => o.ushortArray);
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

            Field(o => o.byteArray);
            Field(o => o.sbyteArray);
            Field(o => o.ulongArray);
            Field(o => o.uintArray);
            Field(o => o.shortArray);
            Field(o => o.ushortArray);
        }
    }

    public class ScalarsMutation : ObjectGraphType
    {
        public ScalarsMutation()
        {
            Name = "ScalarsMutation";
            Field<ScalarsType>(
                "create",
                arguments: new QueryArguments(
                    new QueryArgument<ScalarsInput> { Name = "input" },
                    new QueryArgument<StringGraphType> { Name = "binary" }
                ),
                resolve: ctx =>
                {
                    // additional test for https://github.com/graphql-dotnet/graphql-dotnet/issues/956
                    ctx.GetArgument<string>("binary").ShouldBe("R3JhcGhRTCE="); // GraphQL!
                    ctx.GetArgument<List<byte>>("binary").Count.ShouldBe(12);
                    Encoding.UTF8.GetString(ctx.GetArgument<byte[]>("binary")).ShouldBe("GraphQL!");
                    // end of test

                    var arg = ctx.GetArgument<ScalarsModel>("input");
                    return arg;
                });
        }
    }
}
