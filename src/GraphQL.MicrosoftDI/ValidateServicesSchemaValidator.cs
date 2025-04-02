using System.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// A schema validator that ensures all services required by <see cref="FromServicesAttribute"/> are registered.
/// </summary>
internal sealed class ValidateServicesSchemaValidator : BaseSchemaNodeVisitor
{
    private readonly Func<Type, bool> _isValidService;
    private List<string>? _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateServicesSchemaValidator"/> class.
    /// </summary>
    private ValidateServicesSchemaValidator(Func<Type, bool> isValidService)
    {
        _isValidService = isValidService;
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        var serviceTypes = field.GetMetadata<List<Type>>(FromServicesAttribute.REQUIRED_SERVICES_METADATA);
        if (serviceTypes != null)
        {
            foreach (var serviceType in serviceTypes)
            {
                if (!_isValidService(serviceType))
                {
                    _errors ??= new();
                    _errors.Add($"The service '{serviceType.FullName}' required by '{type.Name}.{field.Name}' is not registered.");
                }
            }
        }
    }

    /// <inheritdoc/>
    public override void PostVisitSchema(ISchema schema)
    {
        if (_errors != null)
        {
            if (_errors.Count == 1)
                throw new InvalidOperationException(_errors[0]);
            else
                throw new InvalidOperationException($"The following service validation errors were found:{Environment.NewLine}{string.Join(Environment.NewLine, _errors)}");
        }
    }

    /// <summary>
    /// Attempts to create a new instance of <see cref="ValidateServicesSchemaValidator"/> for the specified service provider.
    /// If the service provider implements IServiceProviderIsService, then a new instance will be returned.
    /// </summary>
    internal static ValidateServicesSchemaValidator? TryCreate(IServiceProvider serviceProvider)
    {
        try
        {
            var diProvider = serviceProvider.GetService<IServiceProvider>();
            if (diProvider == null)
                return null;

            var providerType = diProvider.GetType();

            // Get the assembly that contains IServiceCollection
            var assembly = typeof(IServiceCollection).Assembly;

            // Find the IServiceProviderIsService type in that assembly
            var isServiceType = assembly.GetType("Microsoft.Extensions.DependencyInjection.IServiceProviderIsService");
            if (isServiceType == null)
                return null;

            // Check if the provider implements the interface
            var serviceProviderIsService = diProvider.GetService(isServiceType);
            if (serviceProviderIsService == null)
                return null;

            var isValidServiceMethod = isServiceType.GetMethod("IsService", BindingFlags.Instance | BindingFlags.Public, null, [typeof(Type)], null);
            if (isValidServiceMethod == null || isValidServiceMethod.ReturnType != typeof(bool))
                return null;

            var fn = (Func<Type, bool>)isValidServiceMethod.CreateDelegate(typeof(Func<Type, bool>), serviceProviderIsService);
            return new ValidateServicesSchemaValidator(fn);
        }
        catch (AmbiguousMatchException)
        {
            return null;
        }
    }
}
