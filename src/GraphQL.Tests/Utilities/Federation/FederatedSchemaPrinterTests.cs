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
            var schema = FederatedSchema.For(@"type X @key(fields: ""id"") { id: ID! }");
            SchemaPrinterOptions options = default;

            schema.Initialize();

            var query = schema.Query;
            var federatedSchemaPrinter = new FederatedSchemaPrinter(schema, options);

            // Act
            string result = federatedSchemaPrinter.PrintObject(query);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
