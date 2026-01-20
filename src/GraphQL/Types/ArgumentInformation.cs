using System.Reflection;

namespace GraphQL.Types;

/// <summary>
/// Contains information pertaining to a method parameter in preparation for building a query argument for it.
/// </summary>
public class ArgumentInformation
{
    /// <summary>
    /// Initializes a new instance with the specified parameters.
    /// </summary>
    public ArgumentInformation(ParameterInfo parameterInfo, Type? sourceType, FieldType? fieldType, TypeInformation typeInformation)
    {
        ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        FieldType = fieldType; // ?? throw new ArgumentNullException(nameof(fieldType));
        SourceType = sourceType; // ?? throw new ArgumentNullException(nameof(sourceType));
        TypeInformation = typeInformation ?? throw new ArgumentNullException(nameof(typeInformation));
    }

    /// <summary>
    /// The method parameter.
    /// </summary>
    public ParameterInfo ParameterInfo { get; }

    /// <summary>
    /// The expected type of <see cref="IResolveFieldContext.Source"/>.
    /// Should equal <c>TSourceType</c> within <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>.
    /// </summary>
    public Type? SourceType { get; }

    /// <summary>
    /// The <see cref="Types.FieldType"/> that the query argument will be added to.
    /// </summary>
    public FieldType? FieldType { get; }

    /// <summary>
    /// The parsed type information of the method parameter.
    /// </summary>
    public TypeInformation TypeInformation { get; }

    /// <summary>
    /// Applies <see cref="GraphQLAttribute"/> attributes pulled from the <see cref="ArgumentInformation.ParameterInfo">ParameterInfo</see> onto this instance.
    /// Also scans the parameter's owning module and assembly for globally-applied attributes.
    /// </summary>
    public virtual void ApplyAttributes()
    {
        var attributes = ParameterInfo.GetGraphQLAttributes();
        foreach (var attr in attributes)
        {
            attr.Modify(this);
        }
    }

    /// <summary>
    /// Builds a query argument from this instance.
    /// The query argument will be added to the arguments list of the field type.
    /// </summary>
    public virtual QueryArgument ConstructQueryArgument()
    {
        var type = TypeInformation.ConstructGraphType();
        var memberType = ParameterInfo.ParameterType;
        var argument = new QueryArgument(type)
        {
            Name = ParameterInfo.Name!,
            Description = ParameterInfo.Description(),
            DefaultValue = ParameterInfo.IsOptional ? ParameterInfo.DefaultValue : null,
        };
        argument.Parser = (value, vc) => memberType.IsInstanceOfType(value) ? value : vc.GetPropertyValue(value, memberType, argument.ResolvedType!)!;
        return argument;
    }
}
