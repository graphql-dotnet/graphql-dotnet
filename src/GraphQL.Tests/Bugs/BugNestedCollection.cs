using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug850MutationAlias : QueryTestBase<NestedMutationSchema>
{
    [Fact]
    public void supports_nested_objects()
    {
        var inputs =
           @"{  ""program"":
                    {   ""name"": ""TEST Program"",
                        ""modifiedBy"": ""TEST"",
                        ""isActive"": true,
                        ""description"": ""Testing from graphql explorer"",
                        ""messageNamespace"": ""http://foo.bar"",
                        ""messageRoot"": ""Foo"",
                        ""steps"": [
                                    { ""programStepDefinitionId"": 1,
                                      ""sequenceOrder"": 1,
                                      ""properties"": [ {""stepPropertyId"": 1, ""propertyValue"": ""60"" } ] },
                                    { ""programStepDefinitionId"": 2,
                                      ""sequenceOrder"": 2,
                                      ""properties"": [] }
                        ]  }}"
                .ToInputs();

        string query = @"mutation createProgram($program: ProgramInput!) { createProgram(program: $program) }";

        string expected = @"{ ""createProgram"": true }";

        AssertQuerySuccess(query, expected, inputs);
    }
}

public class NestedMutationSchema : Schema
{
    public NestedMutationSchema()
    {
        Mutation = new NestedMutation();
    }
}

public class NestedMutation : ObjectGraphType
{
    public NestedMutation()
    {
        Name = "mutation";
        Field<BooleanGraphType>("createProgram",
            arguments: new QueryArguments(new QueryArgument<NonNullGraphType<ProgramInputType>> { Name = "program" }),
            resolve: context =>
            {
                var program = context.GetArgument<Program>("program");
                program.Steps.Any(step => step.PropertyValues.Count > 0).ShouldBeTrue();
                return true;
            });
    }
}

public class ProgramInputType : InputObjectGraphType<Program>
{
    public ProgramInputType()
    {
        Name = "ProgramInput";
        Field(x => x.Name);
        Field(x => x.ModifiedBy);
        Field(x => x.IsActive, type: typeof(BooleanGraphType));
        Field(x => x.MessageNamespace);
        Field(x => x.MessageRoot);
        Field(x => x.Description);
        Field(x => x.Steps, type: typeof(NonNullGraphType<ListGraphType<ProgramStepInputType>>));
    }
}

public class ProgramStepInputType : InputObjectGraphType<ProgramStepConfig>
{
    public ProgramStepInputType()
    {
        Name = "ProgramStepInput";
        Field(x => x.ProgramStepDefinitionId);
        Field(x => x.SequenceOrder);
        Field("properties", x => x.PropertyValues, // Here is a field with a name different from the name of the class property
            type: typeof(ListGraphType<ProgramStepPropertyValueInputType>));
    }
}

public class ProgramStepPropertyValueInputType : InputObjectGraphType<ProgramPropertyValue>
{
    public ProgramStepPropertyValueInputType()
    {
        Name = "ProgramStepPropertyValueInput";
        Field(x => x.StepPropertyId);
        Field(x => x.PropertyValue);
    }
}

public class Program
{
    public int ProgramId { get; set; }
    public string Name { get; set; }
    public string MessageNamespace { get; set; }
    public string MessageRoot { get; set; }
    public bool? IsActive { get; set; }
    public string Description { get; set; }
    public string ModifiedBy { get; set; }
    public System.Collections.Generic.List<ProgramStepConfig> Steps { get; set; }
}

public class ProgramStepConfig
{
    public int SequenceOrder { get; set; }
    public int ProgramStepId { get; set; }
    public System.Collections.Generic.List<ProgramPropertyValue> PropertyValues { get; set; } = new System.Collections.Generic.List<ProgramPropertyValue>();
    public int ProgramStepDefinitionId { get; set; }
}

public class ProgramPropertyValue
{
    public int StepPropertyId { get; set; }
    public string PropertyValue { get; set; }
    public int Id { get; set; }
}
