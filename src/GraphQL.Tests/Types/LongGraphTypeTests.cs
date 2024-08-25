using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class LongGraphTypeTests
{
    private readonly LongGraphType type = new();

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList)
    {
        type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        type.SerializeList(list).ShouldBe(list);
    }

    public static object?[][] SerializeListData = [
        [new List<long> { 1, 2 }, true, true],
        [new List<long?> { 1, 2 }, true, true],
        [new List<long?> { 1, 2, null }, true, false],
        [new long[] { 1, 2 }, true, true],
        [new long?[] { 1, 2 }, true, true],
        [new long?[] { 1, 2, null }, true, false],
        [new ulong[] { 1, 2 }, false, false],
    ];
}
