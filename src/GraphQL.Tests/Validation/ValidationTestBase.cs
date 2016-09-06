using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Should;

namespace GraphQL.Tests.Validation
{
    public class ValidationTestBase<TRule, TSchema>
        where TRule : IValidationRule, new()
        where TSchema : ISchema, new()
    {
        public ValidationTestBase()
        {
            Rule = new TRule();
            Schema = new TSchema();
        }

        protected TRule Rule { get; }

        protected TSchema Schema { get; }

        protected void ShouldFailRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rule(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            var result = Validate(config.Query, Schema, config.Rules);

            result.IsValid.ShouldBeFalse("Expected validation errors though there were none.");
            result.Errors.Count.ShouldEqual(
                config.Assertions.Count(),
                $"The number of errors found ({result.Errors.Count}) does not match the number of errors expected ({config.Assertions.Count()}).");

            config.Assertions.Apply((assert, idx) =>
            {
                var error = result.Errors.Skip(idx).First();
                error.Message.ShouldEqual(assert.Message);

                var allLocations = string.Join("", error.Locations.Select(l => $"({l.Line},{l.Column})"));

                assert.Locations.Apply((assertLoc, locIdx) =>
                {
                    var errorLoc = error.Locations.Skip(locIdx).First();
                    errorLoc.Line.ShouldEqual(
                        assertLoc.Line,
                        $"Expected line {assertLoc.Line} but was {errorLoc.Line} - {error.Message} {allLocations}");
                    errorLoc.Column.ShouldEqual(
                        assertLoc.Column,
                        $"Expected column {assertLoc.Column} but was {errorLoc.Column} - {error.Message} {allLocations}");
                });

                error.Locations.Count().ShouldEqual(assert.Locations.Count());
            });
        }

        protected void ShouldPassRule(string query)
        {
            ShouldPassRule(_ => _.Query = query);
        }

        protected void ShouldPassRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rule(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            var result = Validate(config.Query, Schema, config.Rules);
            var message = "";
            if (result.Errors?.Any() == true)
            {
                message = string.Join(", ", result.Errors.Select(x => x.Message));
            }
            result.IsValid.ShouldBeTrue(message);
        }

        private IValidationResult Validate(string query, ISchema schema, IEnumerable<IValidationRule> rules)
        {
            var documentBuilder = new GraphQLDocumentBuilder();
            var document = documentBuilder.Build(query);
            var validator = new DocumentValidator();
            return validator.Validate(query, schema, document, rules);
        }
    }
}
