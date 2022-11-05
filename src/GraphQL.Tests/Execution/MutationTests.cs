using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class NumberHolder
{
    public int TheNumber { get; set; }
}

public class DateTimeHolder
{
    public DateTime TheDateTime { get; set; }
}

public class Root
{
    public Root(int number, DateTime dateTime)
    {
        NumberHolder = new NumberHolder { TheNumber = number };
        DateTimeHolder = new DateTimeHolder { TheDateTime = dateTime };
    }

    public NumberHolder NumberHolder { get; private set; }

    public DateTimeHolder DateTimeHolder { get; private set; }

    public NumberHolder ImmediatelyChangeTheNumber(int number)
    {
        NumberHolder.TheNumber = number;
        return NumberHolder;
    }

    public Task<NumberHolder> PromiseToChangeTheNumberAsync(int number)
    {
        NumberHolder.TheNumber = number;
        return Task.FromResult(NumberHolder);
    }

    public NumberHolder FailToChangeTheNumber(int number)
    {
        throw new InvalidOperationException($"Cannot change the number {number}");
    }

    public static async Task<NumberHolder> PromiseAndFailToChangeTheNumberAsync(int number)
    {
        await Task.Delay(100).ConfigureAwait(false);
        throw new InvalidOperationException($"Cannot change the number {number}");
    }

    public DateTimeHolder ImmediatelyChangeTheDateTime(DateTime dateTime)
    {
        DateTimeHolder.TheDateTime = dateTime;
        return DateTimeHolder;
    }

    public Task<DateTimeHolder> PromiseToChangeTheDateTimeAsync(DateTime dateTime)
    {
        DateTimeHolder.TheDateTime = dateTime;
        return Task.FromResult(DateTimeHolder);
    }

    public DateTimeHolder FailToChangeTheDateTime()
    {
        throw new InvalidOperationException("Cannot change the datetime");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
    public static async Task<DateTimeHolder> PromiseAndFailToChangeTheDateTimeAsync(DateTime dateTime)
    {
        await Task.Delay(100).ConfigureAwait(false);
        throw new InvalidOperationException("Cannot change the datetime");
    }
}

public class MutationSchema : Schema
{
    public MutationSchema()
    {
        Query = new MutationQuery();
        Mutation = new MutationChange();
    }
}

public class NumberHolderType : ObjectGraphType
{
    public NumberHolderType()
    {
        Name = "NumberHolder";
        Field<IntGraphType>("theNumber");
    }
}

public class DateTimeHolderType : ObjectGraphType
{
    public DateTimeHolderType()
    {
        Name = "DateTimeHolder";
        Field<DateTimeGraphType>("theDateTime");
    }
}

public class GuidHolderType : ObjectGraphType
{
    public GuidHolderType()
    {
        Name = "GuidHolder";
        Field<GuidGraphType>("theGuid").Resolve(x => x.Source);
    }
}

public class MutationQuery : ObjectGraphType
{
    public MutationQuery()
    {
        Name = "Query";
        Field<NumberHolderType>("numberHolder");
        Field<DateTimeHolderType>("dateTimeHolder");
    }
}

public class MutationChange : ObjectGraphType
{
    public MutationChange()
    {
        Name = "Mutation";

        Field<NumberHolderType>("immediatelyChangeTheNumber")
            .Argument<IntGraphType>("newNumber", arg => arg.DefaultValue = 0)
            .Resolve(context =>
            {
                var root = context.Source as Root;
                var change = context.GetArgument<int>("newNumber");
                return root.ImmediatelyChangeTheNumber(change);
            }
        );

        Field<NumberHolderType>("promiseToChangeTheNumber")
            .Argument<IntGraphType>("newNumber", arg => arg.DefaultValue = 0)
            .ResolveAsync(async context =>
            {
                var root = context.Source as Root;
                var change = context.GetArgument<int>("newNumber");
                return await root.PromiseToChangeTheNumberAsync(change).ConfigureAwait(false);
            }
        );

        Field<NumberHolderType>("failToChangeTheNumber")
            .Argument<IntGraphType>("newNumber", arg => arg.DefaultValue = 0)
            .Resolve(context =>
            {
                var root = context.Source as Root;
                var change = context.GetArgument<int>("newNumber");
                return root.FailToChangeTheNumber(change);
            }
        );

        Field<NumberHolderType>("promiseAndFailToChangeTheNumber")
            .Argument<IntGraphType>("newNumber", arg => arg.DefaultValue = 0)
            .ResolveAsync(async context =>
            {
                var change = context.GetArgument<int>("newNumber");
                return await Root.PromiseAndFailToChangeTheNumberAsync(change).ConfigureAwait(false);
            }
        );

        Field<DateTimeHolderType>("immediatelyChangeTheDateTime")
            .Argument<DateTimeGraphType>("newDateTime")
            .Resolve(context =>
            {
                var root = context.Source as Root;
                var change = context.GetArgument<DateTime>("newDateTime");
                return root.ImmediatelyChangeTheDateTime(change);
            }
        );

        Field<DateTimeHolderType>("promiseToChangeTheDateTime")
            .Argument<DateTimeGraphType>("newDateTime")
            .ResolveAsync(async context =>
            {
                var root = context.Source as Root;
                var change = context.GetArgument<DateTime>("newDateTime");
                return await root.PromiseToChangeTheDateTimeAsync(change).ConfigureAwait(false);
            }
        );

        Field<DateTimeHolderType>("failToChangeTheDateTime")
            .Argument<DateTimeGraphType>("newDateTime")
            .Resolve(context =>
            {
                var root = context.Source as Root;
                _ = context.GetArgument<DateTime>("newDateTime");
                return root.FailToChangeTheDateTime();
            }
        );

        Field<DateTimeHolderType>("promiseAndFailToChangeTheDateTime")
            .Argument<DateTimeGraphType>("newDateTime")
            .ResolveAsync(async context =>
            {
                var change = context.GetArgument<DateTime>("newDateTime");
                return await Root.PromiseAndFailToChangeTheDateTimeAsync(change).ConfigureAwait(false);
            }
        );

        Field<GuidHolderType>("passGuidGraphType")
            .Argument<GuidGraphType>("guid")
            .Resolve(context =>
            {
                var guid = context.GetArgument<Guid>("guid");
                return guid;
            }
        );
    }
}

public class MutationTests : QueryTestBase<MutationSchema>
{
    [Fact]
    public void evaluates_mutations_serially()
    {
        var query = @"
                mutation M {
                  first: immediatelyChangeTheNumber(newNumber: 1) {
                    theNumber
                  }
                  second: immediatelyChangeTheNumber(newNumber: 2) {
                    theNumber
                  }
                  third: immediatelyChangeTheNumber(newNumber: 3) {
                    theNumber
                  }
                  fourth: immediatelyChangeTheNumber(newNumber: 4) {
                    theNumber
                  }
                  fifth: immediatelyChangeTheNumber(newNumber: 5) {
                    theNumber
                  }
                }
            ";

        var expected = @"
                {
                  ""first"": {
                    ""theNumber"": 1
                  },
                  ""second"": {
                    ""theNumber"": 2
                  },
                  ""third"": {
                    ""theNumber"": 3
                  },
                  ""fourth"": {
                    ""theNumber"": 4
                  },
                  ""fifth"": {
                    ""theNumber"": 5
                  }
                }";

        AssertQuerySuccess(query, expected, root: new Root(6, DateTime.Now));
    }

    [Fact]
    public async Task evaluates_mutations_correctly_in_the_presence_of_a_failed_mutation()
    {
        var query = @"
                mutation M {
                  first: immediatelyChangeTheNumber(newNumber: 1) {
                    theNumber
                  }
                  second: promiseToChangeTheNumber(newNumber: 2) {
                    theNumber
                  }
                  third: failToChangeTheNumber(newNumber: 3) {
                    theNumber
                  }
                  fourth: promiseToChangeTheNumber(newNumber: 4) {
                    theNumber
                  }
                  fifth: immediatelyChangeTheNumber(newNumber: 5) {
                    theNumber
                  }
                  sixth: promiseAndFailToChangeTheNumber(newNumber: 6) {
                    theNumber
                  }
                }
            ";

        var expected = @"
                {
                  ""first"": {
                    ""theNumber"": 1
                  },
                  ""second"": {
                    ""theNumber"": 2
                  },
                  ""third"": null,
                  ""fourth"": {
                    ""theNumber"": 4
                  },
                  ""fifth"": {
                    ""theNumber"": 5
                  },
                  ""sixth"": null
                }";

        var result = await AssertQueryWithErrorsAsync(query, expected, root: new Root(6, DateTime.Now), expectedErrorCount: 2).ConfigureAwait(false);
        result.Errors.First().InnerException.Message.ShouldBe("Cannot change the number 3");
        var last = result.Errors.Last();
        last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the number 6");
    }

    [Fact]
    public void evaluates_datetime_mutations_serially()
    {
        var query = @"
                mutation M {
                  first: immediatelyChangeTheDateTime(newDateTime: ""2017-01-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  second: immediatelyChangeTheDateTime(newDateTime: ""2017-02-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  third: immediatelyChangeTheDateTime(newDateTime: ""2017-03-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  fourth: immediatelyChangeTheDateTime(newDateTime: ""2017-04-27T15:19:53.123-5:00"") {
                    theDateTime
                  }
                  fifth: immediatelyChangeTheDateTime(newDateTime: ""2017-05-27T15:19:53.123+2:00"") {
                    theDateTime
                  }
                }
            ";

        var expected = @"
                {
                  ""first"": {
                    ""theDateTime"": ""2017-01-27T15:19:53.123Z""
                  },
                  ""second"": {
                    ""theDateTime"": ""2017-02-27T15:19:53.123Z""
                  },
                  ""third"": {
                    ""theDateTime"": ""2017-03-27T15:19:53.123Z""
                  },
                  ""fourth"": {
                    ""theDateTime"": ""2017-04-27T20:19:53.123Z""
                  },
                  ""fifth"": {
                    ""theDateTime"": ""2017-05-27T13:19:53.123Z""
                  }
                }";

        AssertQuerySuccess(query, expected, root: new Root(6, DateTime.Now));
    }

    [Fact]
    public async Task evaluates_datetime_mutations_correctly_in_the_presence_of_a_failed_mutation()
    {
        var query = @"
                mutation M {
                  first: immediatelyChangeTheDateTime(newDateTime: ""2017-01-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  second: promiseToChangeTheDateTime(newDateTime: ""2017-02-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  third: failToChangeTheDateTime(newDateTime: ""2017-03-27T15:19:53.123Z"") {
                    theDateTime
                  }
                  fourth: promiseToChangeTheDateTime(newDateTime: ""2017-04-27T15:19:53.123-5:00"") {
                    theDateTime
                  }
                  fifth: immediatelyChangeTheDateTime(newDateTime: ""2017-05-27T15:19:53.123+2:00"") {
                    theDateTime
                  }
                  sixth: promiseAndFailToChangeTheDateTime(newDateTime: ""2017-06-27T15:19:53.123Z"") {
                    theDateTime
                  }
                }
            ";

        var expected = @"
                {
                  ""first"": {
                    ""theDateTime"": ""2017-01-27T15:19:53.123Z""
                  },
                  ""second"": {
                    ""theDateTime"": ""2017-02-27T15:19:53.123Z""
                  },
                  ""third"": null,
                  ""fourth"": {
                    ""theDateTime"": ""2017-04-27T20:19:53.123Z""
                  },
                  ""fifth"": {
                    ""theDateTime"": ""2017-05-27T13:19:53.123Z""
                  },
                  ""sixth"": null
                }";

        var result = await AssertQueryWithErrorsAsync(query, expected, root: new Root(6, DateTime.Now), expectedErrorCount: 2).ConfigureAwait(false);
        result.Errors.First().InnerException.Message.ShouldBe("Cannot change the datetime");
        var last = result.Errors.Last();
        last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the datetime");
    }

    [Fact]
    public void successfully_handles_guidgraphtype()
    {
        var query = @"
                mutation M {
                  passGuidGraphType(guid: ""085A38AD-907B-4625-AFEE-67EFC71217DE"") {
                    theGuid
                  }
                }
            ";

        var expected = @"{
                    ""passGuidGraphType"": {
                        ""theGuid"": ""085a38ad-907b-4625-afee-67efc71217de""
                    }
                }";

        AssertQuerySuccess(query, expected, root: new Root(6, DateTime.Now));
    }
}
