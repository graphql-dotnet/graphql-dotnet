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

    public class MyObject
    {
        public string Prop1 { get; set; }
        public int Prop2 { get; set; }
    }

    public class MyObjectHolder
    {
        public MyObject TheObject { get; set; }

        public MyObjectHolder()
        {
            TheObject = new MyObject();
        }
    }

    public class Root
    {
        public Root(int number)
        {
            NumberHolder = new NumberHolder { TheNumber = number };
        }

        public NumberHolder NumberHolder { get; private set; }

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
    }

    public class ObjectRoot
    {
        public ObjectRoot(MyObjectHolder obj)
        {
            ObjectHolder = new MyObjectHolder { TheObject = { Prop1 = "", Prop2 = 0 } };
        }

        public MyObjectHolder ObjectHolder { get; private set; }

        public MyObjectHolder ImmediatelyChangeTheObject(MyObject change)
        {
            ObjectHolder.TheObject.Prop1 = change.Prop1;
            ObjectHolder.TheObject.Prop2 = change.Prop2;
            return ObjectHolder;
        }

        public Task<MyObjectHolder> PromiseToChangeTheObjectAsync(MyObject change)
        {
            ObjectHolder.TheObject.Prop1 = change.Prop1;
            ObjectHolder.TheObject.Prop2 = change.Prop2;
            return Task.FromResult(ObjectHolder);
        }

        public MyObjectHolder FailToChangeTheObject(MyObject change)
        {
            throw new InvalidOperationException("Cannot change the object");
        }

        public async Task<MyObjectHolder> PromiseAndFailToChangeTheObjectAsync(MyObject change)
        {
            await Task.Delay(100).ConfigureAwait(false);
            throw new InvalidOperationException("Cannot change the object");
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

    public class ObjectMutationSchema : Schema
    {
        public ObjectMutationSchema()
        {
            Query = new ObjectMutationQuery();
            Mutation = new ObjectMutationChange();
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

    public class MyObjectType : ObjectGraphType<MyObject>, IInputObjectGraphType
    {
        public MyObjectType()
        {
            Name = "MyObject";
            Field(_ => _.Prop1);
            Field(_ => _.Prop2);
        }
    }

    public class ObjectHolderType : ObjectGraphType<MyObjectHolder>, IInputObjectGraphType
    {
        public ObjectHolderType()
        {
            Name = "ObjectHolder";
            Field(_ => _.TheObject, false, typeof(MyObjectType));
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

    public class ObjectMutationQuery : ObjectGraphType
    {
        public ObjectMutationQuery()
        {
            Name = "ObjectQuery";
            Field<ObjectHolderType>("objectHolder");
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
        }
    }

    public class ObjectMutationChange : ObjectGraphType
    {
        public ObjectMutationChange()
        {
            Name = "Mutation";

            Field<ObjectHolderType>(
                "immediatelyChangeTheObject",
                arguments: new QueryArguments(
                    new QueryArgument<MyObjectType>
                    {
                        Name = "newObject",
                        DefaultValue = new MyObjectType()
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as ObjectRoot;
                    var change = context.GetArgument<MyObject>("newObject");
                    return root.ImmediatelyChangeTheObject(change);
                }
            );

            Field<ObjectHolderType>(
                "promiseToChangeTheObject",
                arguments: new QueryArguments(
                    new QueryArgument<MyObjectType>
                    {
                        Name = "newObject",
                        DefaultValue = new MyObject()
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as ObjectRoot;
                    var change = context.GetArgument<MyObject>("newObject");
                    return root.PromiseToChangeTheObjectAsync(change);
                }
            );

            Field<ObjectHolderType>(
                "failToChangeTheObject",
                arguments: new QueryArguments(
                    new QueryArgument<MyObjectType>
                    {
                        Name = "newObject",
                        DefaultValue = new MyObject()
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as ObjectRoot;
                    var change = context.GetArgument<MyObject>("newObject");
                    return root.FailToChangeTheObject(change);
                }
            );

            Field<ObjectHolderType>(
                "promiseAndFailToChangeTheObject",
                arguments: new QueryArguments(
                    new QueryArgument<MyObjectType>
                    {
                        Name = "newObject",
                        DefaultValue = new MyObject()
                    }
                ),
                resolve: context =>
                {
                    var root = context.Source as ObjectRoot;
                    var change = context.GetArgument<MyObject>("newObject");
                    return root.PromiseAndFailToChangeTheObjectAsync(change);
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
            result.Errors.First().InnerException.Message.ShouldBe("Cannot change the number");
            var last = result.Errors.Last();
            last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the number");
        }
    }

    public class ObjectMutationTests : QueryTestBase<ObjectMutationSchema>
    {
        [Fact]
        public void evaluates_object_mutations_serially()
        {
            var query = @"
                mutation M {
                  first: immediatelyChangeTheObject(newObject:  { prop1: ""Mod1"", prop2: 1 }) 
                    {
                        theObject
                        {
                            prop1
                            prop2
                        }
                    }
                  second: immediatelyChangeTheObject(newObject:  { prop1: ""Mod2"", prop2: 2 }) 
                    {
                        theObject
                        {
                            prop1
                            prop2
                        }
                    }
                  third: immediatelyChangeTheObject(newObject:  { prop1: ""Mod3"", prop2: 3 }) 
                    {
                        theObject
                        {
                            prop1
                            prop2
                        }
                    }
                  fourth: immediatelyChangeTheObject(newObject:  { prop1: ""Mod4"", prop2: 4 }) 
                    {
                        theObject
                        {
                            prop1
                            prop2
                        }
                    }
                  fifth: immediatelyChangeTheObject(newObject:  { prop1: ""Mod5"", prop2: 5 }) 
                    {
                        theObject
                        {
                            prop1
                            prop2
                        }
                    }
                }
            ";

            var expected = @"
                {
                    'first': {
                        'theObject': { ""prop1"": ""Mod1"", ""prop2"": 1 }
                    },
                    'second': {
                        'theObject': { ""prop1"": ""Mod2"", ""prop2"": 2 }
                    },
                    'third': {
                        'theObject': { ""prop1"": ""Mod3"", ""prop2"": 3 }
                    },
                    'fourth': {
                        'theObject': { ""prop1"": ""Mod4"", ""prop2"": 4 }
                    },
                    'fifth': {
                        'theObject': { ""prop1"": ""Mod5"", ""prop2"": 5 }
                    },
                }";

            AssertQuerySuccess(query, expected, root: new ObjectRoot(new MyObjectHolder()));
        }

        [Fact]
        public void evaluates_mutations_correctly_in_the_presense_of_a_failed_mutation()
        {
            var query = @"
                mutation M {
                  first: immediatelyChangeTheObject(newObject:  { prop1: ""Mod1"", prop2: 1 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                  second: promiseToChangeTheObject(newObject:  { prop1: ""Mod2"", prop2: 2 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                  third: failToChangeTheObject(newObject:  { prop1: ""Mod3"", prop2: 3 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                  fourth: promiseToChangeTheObject(newObject:  { prop1: ""Mod4"", prop2: 4 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                  fifth: immediatelyChangeTheObject(newObject:  { prop1: ""Mod5"", prop2: 5 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                  sixth: promiseAndFailToChangeTheObject(newObject:  { prop1: ""Mod6"", prop2: 6 }) {
                    theObject
                        {
                            prop1
                            prop2
                        }
                  }
                }
            ";

            var expected = @"
                {
                  'first': {
                        'theObject': { ""prop1"": ""Mod1"", ""prop2"": 1 }
                    },
                  'second': {
                        'theObject': { ""prop1"": ""Mod2"", ""prop2"": 2 }
                    },
                  'third': null,
                  'fourth': {
                    'theObject': { ""prop1"": ""Mod4"", ""prop2"": 4 }
                  },
                  'fifth': {
                    'theObject': { ""prop1"": ""Mod5"", ""prop2"": 5 }
                  },
                  'sixth': null
                }";

            var result = AssertQueryWithErrors(query, expected, root: new ObjectRoot(new MyObjectHolder()
            {
                TheObject = new MyObject()
                {
                    Prop1 = "Mod6",
                    Prop2 = 6
                }
            }), expectedErrorCount: 2);
            result.Errors.First().InnerException.Message.ShouldBe("Cannot change the object");
            var last = result.Errors.Last();
            last.InnerException.GetBaseException().Message.ShouldBe("Cannot change the object");
        }
    }
}
