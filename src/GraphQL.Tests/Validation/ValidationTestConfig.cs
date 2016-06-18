using System;
using System.Collections.Generic;
using GraphQL.Validation;

namespace GraphQL.Tests.Validation
{
    public class ValidationTestConfig
    {
        private readonly List<IValidationRule> _rules = new List<IValidationRule>();
        private readonly List<ValidationErrorAssertion> _assertions = new List<ValidationErrorAssertion>();

        public string Query { get; set; }
        public IEnumerable<IValidationRule> Rules => _rules;
        public IEnumerable<ValidationErrorAssertion> Assertions => _assertions;

        public void Error(string message, int? line = null, int? column = null)
        {
            var assertion = new ValidationErrorAssertion
            {
                Message = message,
            };
            if (line.HasValue)
            {
                assertion.Loc(line.Value, column.Value);
            }
            _assertions.Add(assertion);
        }

        public void Error(Action<ValidationErrorAssertion> configure)
        {
            var assertion = new ValidationErrorAssertion();
            configure(assertion);
            _assertions.Add(assertion);
        }

        public void Rule(params IValidationRule[] rules)
        {
            _rules.AddRange(rules);
        }
    }
}
