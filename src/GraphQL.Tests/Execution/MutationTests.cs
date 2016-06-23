using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Execution
{
    public class NumberHolder
    {
        public int TheNumber { get; set; }
    }

    public class Root
    {
        public Root(int number)
        {
            NumberHolder = new NumberHolder {TheNumber = number};
        }

        public NumberHolder NumberHolder { get; private set; }

        public NumberHolder ImmediatelyChangeTheNumber(int number)
        {
            NumberHolder.TheNumber = number;
            return NumberHolder;
        }

        public Task<NumberHolder> PromiseToChangeTheNumber(int number)
        {
            NumberHolder.TheNumber = number;
            return Task.FromResult(NumberHolder);
        }

        public NumberHolder FailToChangeTheNumber(int number)
        {
            throw new InvalidOperationException("Cannot change the number");
        }

        public async Task<NumberHolder> PromiseAndFailToChangeTheNumber(int number)
        {
            await Task.Delay(100);
            throw new InvalidOperationException("Cannot change the number");
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

    public class MutationQuery : ObjectGraphType
    {
        public MutationQuery()
        {
            Name = "Query";
            Field<NumberHolderType>("numberHolder");
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
                    var change = context.Argument<int>("newNumber");
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
                    var change = context.Argument<int>("newNumber");
                    return root.PromiseToChangeTheNumber(change);
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
                    var change = context.Argument<int>("newNumber");
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
                    var change = context.Argument<int>("newNumber");
                    return root.PromiseAndFailToChangeTheNumber(change);
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

            AssertQuerySuccess(query, expected, root: new Root(6));
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

            var result = AssertQueryWithErrors(query, expected, root: new Root(6), expectedErrorCount: 2);
            result.Errors.First().InnerException.Message.ShouldEqual("Cannot change the number");
            result.Errors.Last().InnerException.Message.ShouldEqual("Cannot change the number");
        }
    }
}
