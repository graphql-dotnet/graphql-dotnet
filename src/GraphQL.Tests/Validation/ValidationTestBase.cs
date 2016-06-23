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

            config.Assertions.Apply((assert, idx) =>
            {
                var error = result.Errors.Skip(idx).First();
                error.Message.ShouldEqual(assert.Message);

                assert.Locations.Apply((assertLoc, locIdx) =>
                {
                    var errorLoc = error.Locations.Skip(locIdx).First();
                    errorLoc.Line.ShouldEqual(
                        assertLoc.Line,
                        $"Expected line {assertLoc.Line} does not match error - {error.Message} ({errorLoc.Line},{errorLoc.Column})");
                    errorLoc.Column.ShouldEqual(
                        assertLoc.Column,
                        $"Expected column {assertLoc.Column} does not match error - {error.Message} ({errorLoc.Line},{errorLoc.Column})");
                });

                error.Locations.Count().ShouldEqual(assert.Locations.Count());
            });

            result.IsValid.ShouldBeFalse();
            result.Errors.Count.ShouldEqual(config.Assertions.Count());
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
            if (result.Errors?.Any() == true)
            {
                Console.WriteLine(string.Join(", ", result.Errors.Select(x=>x.Message)));
            }
            result.IsValid.ShouldBeTrue();
        }

        private IValidationResult Validate(string query, ISchema schema, IEnumerable<IValidationRule> rules)
        {
            var documentBuilder = new AntlrDocumentBuilder();
            var document = documentBuilder.Build(query);
            var validator = new DocumentValidator();
            return validator.Validate(schema, document, rules);
        }
    }
}
