using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class GuidGraphTypeTests : ScalarGraphTypeTest<GuidGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData("936DA01F9ABD4d9d80C702AF85C822A")]
    [InlineData("936DA01F-9ABD-4d9d-80C7-02AF85C822A")]
    [InlineData("936DA01F")]
    public void Coerces_Coerces_input_to_format_exception(object input)
        => AssertException<FormatException>(input);

    [Theory]
    [InlineData("936DA01F9ABD4d9d80C702AF85C822A8", "936DA01F-9ABD-4d9d-80C7-02AF85C822A8")]
    [InlineData("936DA01F-9ABD-4d9d-80C7-02AF85C822A8", "936DA01F-9ABD-4d9d-80C7-02AF85C822A8")]
    public void Coerces_Coerces_input_to_valid_guid(object input, string expected)
        => type.ParseValue(input).ShouldBe(new Guid(expected));

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList, string[] expected)
    {
        type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        type.SerializeList(list).ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(expected);
    }

    public static object?[][] SerializeListData = [
        [new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") }, true, true, new string[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "33333333-3333-3333-3333-333333333333" }],
        [new List<Guid?> { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), null }, true, false, new string?[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", null }],
        [new Guid[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") }, true, true, new string[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "33333333-3333-3333-3333-333333333333" }],
        [new Guid?[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), null }, true, false, new string?[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", null }],
    ];
}
