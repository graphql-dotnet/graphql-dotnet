using GraphQL.DI;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphQL;

/// <summary>
/// Adds a schema validator to the schema that ensures all services required by <see cref="FromServicesAttribute"/> are registered.
/// </summary>
internal sealed class ValidateServicesSchemaConfigurator : IConfigureSchema
{
    /// <inheritdoc/>
    public void Configure(ISchema schema, IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ValidateServicesOptions>>();
        if (!options.Value.Enabled)
            return;
        var serviceValidator = ValidateServicesSchemaValidator.TryCreate(serviceProvider);
        if (serviceValidator != null)
            schema.RegisterVisitor(serviceValidator);
        else
            throw new InvalidOperationException("Could not create a service validator. Ensure that the dependency injection framework supports IServiceProviderIsService.");
    }
}
