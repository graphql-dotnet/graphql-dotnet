using System.Reflection;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies a custom parser method for a field argument or a field of an input object in a GraphQL schema.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ParserAttribute : GraphQLAttribute
{
    private const string DEFAULT_METHOD_NAME = "Parse";

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type? _parserType;
    private readonly string _parserMethodName;

    /// <summary>
    /// Specifies a custom parser method for a field of an input object in a GraphQL schema using the specified parser method name.
    /// The method must exist on the declaring type of the field and be declared as static.
    /// The method may be private or public and must have the signature 'object <paramref name="parserMethodName"/>(object value)'.
    /// If it is public, you may wish to mark the method with <see cref="IgnoreAttribute"/> to prevent it from being exposed in the schema.
    /// </summary>
    [RequiresUnreferencedCode("Please ensure the specified method is not trimmed or use an alternative constructor.")]
    public ParserAttribute(string parserMethodName)
    {
        _parserMethodName = parserMethodName
            ?? throw new ArgumentNullException(nameof(parserMethodName));
    }

    /// <summary>
    /// Specifies a custom parser method for a field of an input object in a GraphQL schema using the 'Parse' method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'object Parse(object value)'.
    /// </summary>
    public ParserAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type parserType)
    {
        _parserType = parserType
            ?? throw new ArgumentNullException(nameof(parserType));
        _parserMethodName = DEFAULT_METHOD_NAME;
    }

    /// <summary>
    /// Specifies a custom parser method for a field of an input object in a GraphQL schema using the specified method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'object <paramref name="parserMethodName"/>(object value)'.
    /// </summary>
    public ParserAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type parserType, string parserMethodName)
    {
        _parserType = parserType ?? throw new ArgumentNullException(nameof(parserType));
        _parserMethodName = parserMethodName ?? throw new ArgumentNullException(nameof(parserMethodName));
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The parser type is marked with DynamicallyAccessedMembers.PublicMethods")]
    public override void Modify(FieldType fieldType, bool isInputType, IGraphType graphType, MemberInfo memberInfo, ref bool ignore)
    {
        if (!isInputType)
            return;
        var parserType = _parserType ?? memberInfo.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (parserType == memberInfo.DeclaringType!)
            bindingFlags |= BindingFlags.NonPublic;
        // Try to find method with new signature first: (object, IValueConverter)
        var method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object), typeof(IValueConverter)], null);
        if (method != null)
        {
            if (method.ReturnType != typeof(object))
                throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
            fieldType.Parser = method.CreateDelegate<Func<object, IValueConverter, object>>();
        }
        else
        {
            // Fall back to old signature: (object)
            method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object)], null)
                ?? throw new InvalidOperationException($"Could not find method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' while initializing '{graphType.Name}.{fieldType.Name}'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.");
            if (method.ReturnType != typeof(object))
                throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
            var parser = method.CreateDelegate<Func<object, object>>();
            fieldType.Parser = (value, _) => parser(value);
        }
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The parser type is marked with DynamicallyAccessedMembers.PublicMethods")]
    public override void Modify(QueryArgument queryArgument, ParameterInfo parameterInfo)
    {
        var parserType = _parserType ?? parameterInfo.Member.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (parserType == parameterInfo.Member.DeclaringType)
            bindingFlags |= BindingFlags.NonPublic;
        // Try to find method with new signature first: (object, IValueConverter)
        var method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object), typeof(IValueConverter)], null);
        if (method != null)
        {
            if (method.ReturnType != typeof(object))
                throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
            queryArgument.Parser = method.CreateDelegate<Func<object, IValueConverter, object>>();
        }
        else
        {
            // Fall back to old signature: (object)
            method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object)], null)
                ?? throw new InvalidOperationException($"Could not find method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' while initializing argument '{queryArgument.Name}'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.");
            if (method.ReturnType != typeof(object))
                throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
            var parser = method.CreateDelegate<Func<object, object>>();
            queryArgument.Parser = (value, _) => parser(value);
        }
    }
}
