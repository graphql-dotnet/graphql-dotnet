using System.Numerics;

namespace GraphQL.Tests.Serialization;

public class DeserializationTestBase
{
    protected readonly TestData ExampleData = new()
    {
        array = new object[]
        {
            null,
            "test",
            123,
            1.2
        },
        obj = new TestChildData
        {
            itemNull = null,
            itemString = "test",
            itemNum = 123,
            itemFloat = 12.4,
        },
        itemNull = null,
        itemString = "test",
        itemNum = 123,
        itemFloat = 12.4,
        itemBigInt = BigInteger.Parse("1234567890123456789012345678901234567890"),
    };
    protected readonly string ExampleJson = "{\"array\":[null,\"test\",123,1.2],\"obj\":{\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.4},\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.4,\"itemBigInt\":1234567890123456789012345678901234567890}";

    public class TestData
    {
        public object[] array { get; set; }
        public TestChildData obj { get; set; }
        public string itemNull { get; set; }
        public string itemString { get; set; }
        public int itemNum { get; set; }
        public double itemFloat { get; set; }
        public BigInteger itemBigInt { get; set; }
    }

    public class TestChildData
    {
        public string itemNull { get; set; }
        public string itemString { get; set; }
        public int itemNum { get; set; }
        public double itemFloat { get; set; }
    }

    protected void Verify(IReadOnlyDictionary<string, object> actual)
    {
        var array = actual["array"].ShouldBeOfType<List<object>>();
        array[0].ShouldBeNull();
        array[1].ShouldBeOfType<string>().ShouldBe((string)ExampleData.array[1]);
        array[2].ShouldBeOfType<int>().ShouldBe((int)ExampleData.array[2]);
        array[3].ShouldBeOfType<double>().ShouldBe((double)ExampleData.array[3]);
        var obj = actual["obj"].ShouldBeOfType<Dictionary<string, object>>();
        obj["itemNull"].ShouldBeNull();
        obj["itemString"].ShouldBeOfType<string>().ShouldBe(ExampleData.obj.itemString);
        obj["itemNum"].ShouldBeOfType<int>().ShouldBe(ExampleData.obj.itemNum);
        obj["itemFloat"].ShouldBeOfType<double>().ShouldBe(ExampleData.obj.itemFloat);
        actual["itemNull"].ShouldBeNull();
        actual["itemString"].ShouldBeOfType<string>().ShouldBe(ExampleData.itemString);
        actual["itemNum"].ShouldBeOfType<int>().ShouldBe(ExampleData.itemNum);
        actual["itemFloat"].ShouldBeOfType<double>().ShouldBe(ExampleData.itemFloat);
        actual["itemBigInt"].ShouldBeOfType<BigInteger>().ShouldBe(ExampleData.itemBigInt);
    }
}
