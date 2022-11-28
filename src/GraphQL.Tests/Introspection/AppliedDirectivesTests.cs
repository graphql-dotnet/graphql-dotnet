using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Introspection;

public class AppliedDirectivesTests
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public async Task introspection_should_return_applied_directives(IGraphQLTextSerializer serializer)
    {
        var documentExecuter = new DocumentExecuter();
        var executionResult = await documentExecuter.ExecuteAsync(_ =>
        {
            _.Schema = new AppliedSchema().EnableExperimentalIntrospectionFeatures(ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution);
            _.Query = "AppliedDirectives".ReadGraphQLRequest();
        }).ConfigureAwait(false);

        var json = serializer.Serialize(executionResult);
        executionResult.Errors.ShouldBeNull();

        json.ShouldBe("AppliedDirectivesResult".ReadJsonResult());
    }

    private class AppliedSchema : Schema
    {
        public AppliedSchema()
        {
            var query = new ObjectGraphType
            {
                Name = "Query"
            };
            query.Field<IntGraphType>("dummy");
            Query = query;
            Mutation = new RootMutation();
            Directives.Register(new TraitsDirective());
            this.ApplyDirective("traits", "quality", "high");
        }
    }

    private class RootMutation : ObjectGraphType
    {
        public RootMutation()
        {
            Field<StringGraphType>("test")
                .Argument<StringGraphType>("some", arg => arg.ApplyDirective("traits", "quality", "moderate"))
                .Directive("traits", "quality", "low");

            this.ApplyDirective("traits"); // default arguments values used, therefore they will not be returned by introspection
        }
    }

    private class TraitsDirective : Directive
    {
        public override bool? Introspectable => true;

        public TraitsDirective()
            : base("traits", DirectiveLocation.Schema, DirectiveLocation.Object, DirectiveLocation.FieldDefinition, DirectiveLocation.ArgumentDefinition)
        {
            Description = "Some traits";
            Arguments = new QueryArguments(new QueryArgument<StringGraphType>
            {
                Name = "quality",
                DefaultValue = "unknown"
            });
        }
    }
}
