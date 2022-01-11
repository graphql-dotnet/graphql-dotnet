using System.Threading.Tasks;
using GraphQL.Types;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Introspection
{
    public class AppliedDirectivesTests
    {
        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task introspection_should_return_applied_directives(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new AppliedSchema().EnableExperimentalIntrospectionFeatures(ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution);
                _.Query = "AppliedDirectives".ReadGraphQLRequest();
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);
            executionResult.Errors.ShouldBeNull();

            json.ShouldBe("AppliedDirectivesResult".ReadJsonResult());
        }

        private class AppliedSchema : Schema
        {
            public AppliedSchema()
            {
                Mutation = new RootMutation();
                Directives.Register(new TraitsDirective());
                this.ApplyDirective("traits", "quality", "high");
            }
        }

        private class RootMutation : ObjectGraphType
        {
            public RootMutation()
            {
                Field<StringGraphType>(
                    "test",
                    arguments: new QueryArguments(new QueryArgument(typeof(StringGraphType)) { Name = "some" }.ApplyDirective("traits", "quality", "moderate"))
                ).ApplyDirective("traits", "quality", "low");

                this.ApplyDirective("traits"); // default arguments values used, therefore they will not be returned by introspection
            }
        }

        private class TraitsDirective : DirectiveGraphType
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
}
