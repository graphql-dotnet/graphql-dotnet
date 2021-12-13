using GraphQL.Utilities;
using GraphQL.Utilities.Federation;
using Xunit;

namespace GraphQL.Tests.Utilities.Federation
{
    public class FederatedSchemaPrinterTests
    {
        [Theory]
        [InlineData(@"type X @key(fields: ""id"") { id: ID! }")]
        [InlineData(@"schema { query: MyQuery } type MyQuery type X @key(fields: ""id"") { id: ID! }")]
        public void PrintObject_ReturnsEmptyString_GivenQueryTypeHasOnlyFederatedFields(string definitions)
        {
            // Arrange
            var schema = FederatedSchema.For(definitions);
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
