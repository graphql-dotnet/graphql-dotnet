using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Should;

namespace GraphQL.Tests.Validation
{
    public class ValidationTestBase<TSchema>
        where TSchema : ISchema, new()
    {
        public ValidationTestBase()
        {
            Schema = new TSchema();
        }

        protected TSchema Schema { get; }

        protected void ShouldFailRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            configure(config);

            var result = Validate(config.Query, Schema, config.Rules);

            var count = 0;

            config.Assertions.Apply(assert =>
            {
                var error = result.Errors.Skip(count).First();
                error.Message.ShouldEqual(assert.Message);

                if (assert.Line != null)
                {
                    var location = error.Locations.Single();
                    location.Line.ShouldEqual(assert.Line.Value);
                    location.Column.ShouldEqual(assert.Column.Value);
                }
                count++;
            });

            result.IsValid.ShouldBeFalse();
        }

        protected void ShouldPassRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            configure(config);

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
