using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Utilities.Federation;
using Xunit;

namespace GraphQL.Tests.Utilities.Federation
{
    public class FederatedSchemaPrinterTests
    {
        [Fact]
        public void PrintObject_ReturnsEmptyString_GivenQueryTypeHasOnlyFederatedFields()
        {
            // Arrange
            ISchema schema = default;
            SchemaPrinterOptions options = default;
            var query = new ObjectGraphType { Name = "Query" };
            query.Field("_entities", new NonNullGraphType(new ListGraphType(new NonNullGraphType(new GraphQLTypeReference("_Any")))));
            query.Field("_service", new NonNullGraphType(new GraphQLTypeReference("_Service")));

            var federatedSchemaPrinter = new FederatedSchemaPrinter(schema, options);

            // Act
            string result = federatedSchemaPrinter.PrintObject(query);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
