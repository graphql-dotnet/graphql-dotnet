using System.Linq;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using Should;

namespace GraphQL.Tests.Validation
{
    public class ScalarLeafTests
    {
        private readonly ScalarLeafs _rule;

        public ScalarLeafTests()
        {
            _rule = new ScalarLeafs();
        }

        [Test]
        public void valid_scalar_selection()
        {
            var schema = new ValidationSchema();

            var query = @"
fragment scalarSelection on Dog {
  barks
}
";

            var result = Validate(query, schema, _rule);

            result.IsValid.ShouldBeTrue();
        }

        [Test]
        public void scalar_selection_not_allowed_on_boolean()
        {
            var schema = new ValidationSchema();

            var query = @"
fragment scalarSelectionNotAllowedOnBoolean on Dog {
  barks { sinceWhen }
}
";

            var result = Validate(query, schema, _rule);

            result.IsValid.ShouldBeFalse();
        }

        [Test]
        public void scalar_selection_not_allowed_on_enum()
        {
            var schema = new ValidationSchema();

            var query = @"
fragment scalarSelectionsNotAllowedOnEnum on Cat {
  furColor { inHexdec }
}
";

            var result = Validate(query, schema, _rule);

            result.IsValid.ShouldBeFalse();
            result.Errors.First().Message.ShouldEqual(_rule.NoSubselectionAllowedMessage("furColor", "FurColor"));
        }

        private IValidationResult Validate(string query, Schema schema, params IValidationRule[] rules)
        {
            var documentBuilder = new AntlrDocumentBuilder();
            var document = documentBuilder.Build(query);
            var validator = new DocumentValidator();
            return validator.Validate(schema, document, rules);
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

    public class ValidationSchema : Schema
    {
        public ValidationSchema()
        {
            Query = new Dog();
            RegisterType<Cat>();
        }
    }
}
