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
  create(input: {id1:""8dfab389-a6f7-431d-ab4e-aa693cc53edf"", id2:""8dfab389-a6f7-431d-ab4e-aa693cc53ede"", uint: 10, short: -20, ushort: 30, ulong: 40, byte: 50, sbyte: -60 })
  {
    id1
    id2
    uint
    short
    ushort
    ulong
    byte
    sbyte
  }
}
";
            var expected = @"{
  ""create"": {
    ""id1"": ""8dfab389-a6f7-431d-ab4e-aa693cc53edf"",
    ""id2"": ""8dfab389-a6f7-431d-ab4e-aa693cc53ede"",
    ""uint"": 10,
    ""short"": -20,
    ""ushort"": 30,
    ""ulong"": 40,
    ""byte"": 50,
    ""sbyte"": -60
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

        public short sHort { get; set; }

        public ushort uShort { get; set; }

        public ulong uLong { get; set; }

        public byte bYte { get; set; }

        public sbyte sByte { get; set; }
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
        }
    }
}
