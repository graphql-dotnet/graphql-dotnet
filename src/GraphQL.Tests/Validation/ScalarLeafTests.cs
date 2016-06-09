using GraphQL.Types;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class ScalarLeafTests : ValidationTestBase<ValidationSchema>
    {
        private readonly ScalarLeafs _rule;

        public ScalarLeafTests()
        {
            _rule = new ScalarLeafs();
        }

        [Test]
        public void valid_scalar_selection()
        {
            var query = @"
fragment scalarSelection on Dog {
  barks
}
";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void object_type_missing_selection()
        {
            var query = @"
query directQueryOnObjectWithoutSubFields{
  human
}
";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(
                    message: _rule.RequiredSubselectionMessage("human", "Human"),
                    line: 3,
                    column: 3);
            });
        }

        [Test]
        public void scalar_selection_not_allowed_on_boolean()
        {
            var query = @"
fragment scalarSelectionNotAllowedOnBoolean on Dog {
  barks { sinceWhen }
}
";
            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(
                    message: _rule.NoSubselectionAllowedMessage("barks", "Boolean"),
                    line: 3,
                    column: 3);
            });
        }

        [Test]
        public void scalar_selection_not_allowed_on_enum()
        {
            var query = @"
fragment scalarSelectionsNotAllowedOnEnum on Cat {
  furColor { inHexdec }
}
";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(
                    message: _rule.NoSubselectionAllowedMessage("furColor", "FurColor"),
                    line: 3,
                    column: 3);
            });
        }
    }

    public class Dog : ObjectGraphType
    {
        public Dog()
        {
            Field<BooleanGraphType>("barks", "");
        }
    }

    public class FurColor : EnumerationGraphType
    {
        public FurColor()
        {
            AddValue("Brown", "", 0);
            AddValue("Yellow", "", 1);
        }
    }

    public class Cat : ObjectGraphType
    {
        public Cat()
        {
            Field<FurColor>("furColor");
        }
    }

    public class Human : ObjectGraphType
    {
        public Human()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> {Name = "surname"}
                ));
            Field<IntGraphType>("iq");
        }
    }

    public class ComplicatedArgs : ObjectGraphType
    {
        public ComplicatedArgs()
        {
            Name = "ComplicatedArgs";
            Field<StringGraphType>(
                "intArgField",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> {Name = "intArg"}
                ));
        }
    }

    public class ValidationQueryRoot : ObjectGraphType
    {
        public ValidationQueryRoot()
        {
            Field<Human>(
                "human",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType>
                    {
                        Name = "id"
                    }
                ));
            Field<Dog>("dog");
            Field<Cat>("cat");
            Field<ComplicatedArgs>("complicatedArgs");
        }
    }

    public class ValidationSchema : Schema
    {
        public ValidationSchema()
        {
            Query = new ValidationQueryRoot();
            RegisterType<Dog>();
            RegisterType<Cat>();
            RegisterType<Human>();
        }
    }
}
