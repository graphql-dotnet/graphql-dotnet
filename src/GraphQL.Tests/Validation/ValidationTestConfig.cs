using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Validation;

public class ValidationTestConfig
{
    private readonly List<IValidationRule> _rules = new List<IValidationRule>();
    private readonly List<ValidationErrorAssertion> _assertions = new List<ValidationErrorAssertion>();

    public ISchema Schema { get; set; }

    public string Query { get; set; }

    public Inputs Variables { get; set; } = Inputs.Empty;

    public IList<IValidationRule> Rules => _rules;

    public IList<ValidationErrorAssertion> Assertions => _assertions;

    public void Error(string message, int line, int column)
    {
        var assertion = new ValidationErrorAssertion
        {
            Message = message,
        };
        assertion.Loc(line, column);
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
