using GraphQL.DI;
using GraphQL.MicrosoftDI;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Adds a schema validator to the schema that ensures all services required by <see cref="FromServicesAttribute"/> are registered.
/// </summary>
internal sealed class ValidateServicesSchemaConfigurator : IConfigureSchema
{
    /// <inheritdoc/>
    public void Configure(ISchema schema, IServiceProvider serviceProvider)
    {
        var serviceValidator = ValidateServicesSchemaValidator.TryCreate(serviceProvider);
        if (serviceValidator != null)
            schema.RegisterVisitor(serviceValidator);
        else
            throw new InvalidOperationException("Could not create a service validator. Ensure that the dependency injection framework supports IServiceProviderIsService.");
    }
}
