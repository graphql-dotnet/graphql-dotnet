using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

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

        var result = Validate(config.Query, config.Schema ?? Schema, config.Rules, config.Variables);

        result.IsValid.ShouldBeFalse("Expected validation errors though there were none.");
        result.Errors.Count.ShouldBe(
            config.Assertions.Count,
            $"The number of errors found ({result.Errors.Count}) does not match the number of errors expected ({config.Assertions.Count}).");

        for (int i = 0; i < config.Assertions.Count; i++)
        {
            var assert = config.Assertions[i];
            var error = result.Errors[i];

            error.Message.ShouldBe(assert.Message);

            var allLocations = string.Concat(error.Locations.Select(l => $"({l.Line},{l.Column})"));
            var locations = error.Locations;

            for (int j = 0; j < assert.Locations.Count; j++)
            {
                var assertLoc = assert.Locations[j];
                var errorLoc = locations[j];

                errorLoc.Line.ShouldBe(
                    assertLoc.Line,
                    $"Expected line {assertLoc.Line} but was {errorLoc.Line} - {error.Message} {allLocations}");
                errorLoc.Column.ShouldBe(
                    assertLoc.Column,
                    $"Expected column {assertLoc.Column} but was {errorLoc.Column} - {error.Message} {allLocations}");
            }

            locations.Count.ShouldBe(assert.Locations.Count);
        }
    }

    protected void ShouldPassRule(string query, string variables = null)
    {
        ShouldPassRule(config =>
        {
            config.Query = query;
            config.Variables = variables.ToInputs();
        });
    }

    protected void ShouldPassRule(Action<ValidationTestConfig> configure)
    {
        var config = new ValidationTestConfig();
        config.Rule(Rule);
        configure(config);

        config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

        var result = Validate(config.Query, config.Schema ?? Schema, config.Rules, config.Variables);
        var message = "";
        if (result.Errors?.Any() == true)
        {
            message = string.Join(", ", result.Errors.Select(x => x.Message));
        }
        result.IsValid.ShouldBeTrue(message);
    }

    private IValidationResult Validate(string query, ISchema schema, IEnumerable<IValidationRule> rules, Inputs variables)
    {
        var documentBuilder = new GraphQLDocumentBuilder();
        var document = documentBuilder.Build(query);
        var validator = new DocumentValidator();
        return validator.ValidateAsync(new ValidationOptions
        {
            Schema = schema,
            Document = document,
            Rules = rules,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().FirstOrDefault(),
            Variables = variables
        }).Result.validationResult;
    }
}
