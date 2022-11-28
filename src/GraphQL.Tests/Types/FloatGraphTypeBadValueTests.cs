using System.Globalization;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/pulls/1781
public class FloatGraphTypeBadValueTests : QueryTestBase<PR1781Schema>
{
    private static readonly string _minNumber = ((FormattableString)$"{double.MinValue:0}0.0").ToString(CultureInfo.InvariantCulture);
    private static readonly string _maxNumber = ((FormattableString)$"{double.MaxValue:0}0.0").ToString(CultureInfo.InvariantCulture);

    [Fact]
    public void BadFloatValues()
    {
        var type = new FloatGraphType();

        var value1 = new GraphQLFloatValue(_maxNumber);
        var value2 = new GraphQLFloatValue(_minNumber);

        type.CanParseLiteral(value1).ShouldBeFalse();
        type.CanParseLiteral(value2).ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => type.ParseLiteral(value1)).Message.ShouldStartWith("Unable to convert '1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0' literal from AST representation to the scalar type 'Float'");
        Should.Throw<InvalidOperationException>(() => type.ParseLiteral(value2)).Message.ShouldStartWith("Unable to convert '-1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0' literal from AST representation to the scalar type 'Float'");
    }

    [Fact]
    public async Task DocumentExecuter_really_big_double_Invalid()
    {
        var de = new DocumentExecuter();
        _maxNumber.ShouldBe("1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0");
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            // create a floating-point value that is larger than double.MaxValue
            // in the expression "{double.MaxValue:0}0.0" below, the 0.0 effectively
            // multiplies double.MaxValue by 10 and the .0 forces the parser to
            // assume it is a floating point value rather than a large integer
            Query = $"{{ test(arg:{_maxNumber}) }}",
            Schema = Schema,
        }).ConfigureAwait(false);
        valid.ShouldNotBeNull();
        valid.Data.ShouldBeNull();
        valid.Errors.ShouldNotBeNull();
        valid.Errors.Count.ShouldBe(1);
        valid.Errors[0].Message.ShouldBe($"Argument 'arg' has invalid value. Expected type 'Float', found {_maxNumber}.");
    }

    [Fact]
    public async Task DocumentExecuter_really_small_double_Invalid()
    {
        var de = new DocumentExecuter();
        _minNumber.ShouldBe("-1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0");
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            Query = $"{{ test(arg:{_minNumber}) }}",
            Schema = Schema,
        }).ConfigureAwait(false);
        valid.ShouldNotBeNull();
        valid.Data.ShouldBeNull();
        valid.Errors.ShouldNotBeNull();
        valid.Errors.Count.ShouldBe(1);
        valid.Errors[0].Message.ShouldBe($"Argument 'arg' has invalid value. Expected type 'Float', found {_minNumber}.");
    }
}

public class PR1781Schema : Schema
{
    public PR1781Schema()
    {
        Query = new PR1781Query();
    }
}

public class PR1781Query : ObjectGraphType
{
    public PR1781Query()
    {
        Field<StringGraphType>("Test")
            .Resolve(_ => "ok")
            .Argument(typeof(FloatGraphType), "arg");
    }
}
