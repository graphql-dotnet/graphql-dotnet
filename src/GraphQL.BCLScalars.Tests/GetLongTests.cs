using System.Globalization;
using GraphQL.Types;

namespace GraphQL.BCLScalars.Tests;

public class GetLongTests
{
    [Fact]
    public void GetLong()
    {
        for (long i = -15L; i <= 15L; ++i)
            LongGraphType.GetLong(i.ToString(CultureInfo.InvariantCulture)).ShouldBe(i);

        LongGraphType.GetLong("-1000000").ShouldBe(-1000000L);
        LongGraphType.GetLong("1000000").ShouldBe(1000000L);

        Should.Throw<FormatException>(() => LongGraphType.GetLong("a")).Message.ShouldEndWith(" was not in a correct format.");
    }
}
