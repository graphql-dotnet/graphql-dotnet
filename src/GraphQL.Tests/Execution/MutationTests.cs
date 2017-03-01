using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
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
            NumberHolder = new NumberHolder {TheNumber = number};
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
            throw new InvalidOperationException("Cannot change the number");
        }

        public async Task<NumberHolder> PromiseAndFailToChangeTheNumberAsync(int number)
        {
            await Task.Delay(100).ConfigureAwait(false);
            throw new InvalidOperationException("Cannot change the number");
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

        public DateTimeHolder FailToChangeTheDateTime(DateTime dateTime)
        {
            throw new InvalidOperationException("Cannot change the datetime");
        }

        public async Task<DateTimeHolder> PromiseAndFailToChangeTheDateTimeAsync(DateTime dateTime)
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
            Field<DateGraphType>("theDateTime");
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

            Field<NumberHolderType>(
                "immediatelyChangeTheNumber",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>
                    {
                        Name = "newNumber",
                        DefaultValue = 0
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<int>("newNumber");
                    return root.ImmediatelyChangeTheNumber(change);
                }
            );

            Field<NumberHolderType>(
                "promiseToChangeTheNumber",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>
                    {
                        Name = "newNumber",
                        DefaultValue = 0
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<int>("newNumber");
                    return root.PromiseToChangeTheNumberAsync(change);
                }
            );

            Field<NumberHolderType>(
                "failToChangeTheNumber",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>
                    {
                        Name = "newNumber",
                        DefaultValue = 0
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<int>("newNumber");
                    return root.FailToChangeTheNumber(change);
                }
            );

            Field<NumberHolderType>(
                "promiseAndFailToChangeTheNumber",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>
                    {
                        Name = "newNumber",
                        DefaultValue = 0
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<int>("newNumber");
                    return root.PromiseAndFailToChangeTheNumberAsync(change);
                }
            );

            Field<DateTimeHolderType>(
                "immediatelyChangeTheDateTime",
                arguments: new QueryArguments(
                    new QueryArgument<DateGraphType>
                    {
                        Name = "newDateTime"
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<DateTime>("newDateTime");
                    return root.ImmediatelyChangeTheDateTime(change);
                }
            );

            Field<DateTimeHolderType>(
                "promiseToChangeTheDateTime",
                arguments: new QueryArguments(
                    new QueryArgument<DateGraphType>
                    {
                        Name = "newDateTime"
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<DateTime>("newDateTime");
                    return root.PromiseToChangeTheDateTimeAsync(change);
                }
            );

            Field<DateTimeHolderType>(
                "failToChangeTheDateTime",
                arguments: new QueryArguments(
                    new QueryArgument<DateGraphType>
                    {
                        Name = "newDateTime"
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<DateTime>("newDateTime");
                    return root.FailToChangeTheDateTime(change);
                }
            );

            Field<DateTimeHolderType>(
                "promiseAndFailToChangeTheDateTime",
                arguments: new QueryArguments(
                    new QueryArgument<DateGraphType>
                    {
                        Name = "newDateTime"
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as Root;
                    var change = context.GetArgument<DateTime>("newDateTime");
                    return root.PromiseAndFailToChangeTheDateTimeAsync(change);
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
                  'first': {
                    'theNumber': 1
                  },
                  'second': {
                    'theNumber': 2
                  },
                  'third': {
                    'theNumber': 3
                  },
                  'fourth': {
                    'theNumber': 4
                  },
                  'fifth': {
                    'theNumber': 5
                  }
                }";

            AssertQuerySuccess(query, expected, root: new Root(6, DateTime.Now));
        }

        [Fact]
        public void evaluates_mutations_correctly_in_the_presense_of_a_failed_mutation()
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
                  'first': {
                    'theNumber': 1
                  },
                  'second': {
                    'theNumber': 2
                  },
                  'third': null,
                  'fourth': {
                    'theNumber': 4
                  },
                  'fifth': {
                    'theNumber': 5
                  },
                  'sixth': null
                }";

            var result = AssertQueryWithErrors(query, expected, root: new Root(6, DateTime.Now), expectedErrorCount: 2);
            result.Errors.First().InnerException.Message.ShouldBe("Cannot change the number");
            var last = result.Errors.Last();
            last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the number");
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
                  'first': {
                    'theDateTime': ""2017-01-27T15:19:53.123Z""
                  },
                  'second': {
                    'theDateTime': ""2017-02-27T15:19:53.123Z""
                  },
                  'third': {
                    'theDateTime': ""2017-03-27T15:19:53.123Z""
                  },
                  'fourth': {
                    'theDateTime': ""2017-04-27T20:19:53.123Z""
                  },
                  'fifth': {
                    'theDateTime': ""2017-05-27T13:19:53.123Z""
                  }
                }";

            AssertQuerySuccess(query, expected, root: new Root(6, DateTime.Now));
        }

        [Fact]
        public void evaluates_datetime_mutations_correctly_in_the_presense_of_a_failed_mutation()
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
                  'first': {
                    'theDateTime': ""2017-01-27T15:19:53.123Z""
                  },
                  'second': {
                    'theDateTime': ""2017-02-27T15:19:53.123Z""
                  },
                  'third': null,
                  'fourth': {
                    'theDateTime': ""2017-04-27T20:19:53.123""
                  },
                  'fifth': {
                    'theDateTime': ""2017-05-27T13:19:53.123""
                  },
                  'sixth': null
                }";

            var result = AssertQueryWithErrors(query, expected, root: new Root(6, DateTime.Now), expectedErrorCount: 2);
            result.Errors.First().InnerException.Message.ShouldBe("Cannot change the datetime");
            var last = result.Errors.Last();
            last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the datetime");
        }
    }
}
