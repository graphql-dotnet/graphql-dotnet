using System.Reflection;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies a custom parser method for a field argument or a field of an input object in a GraphQL schema.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ParserAttribute : GraphQLAttribute
{
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
    /// Specifies a custom parser method for a field of an input object in a GraphQL schema using the 'Parser' method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'object Parser(object value)'.
    /// </summary>
    public ParserAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type parserType)
    {
        _parserType = parserType
            ?? throw new ArgumentNullException(nameof(parserType));
        _parserMethodName = nameof(FieldType.Parser);
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
    public override void Modify(FieldType fieldType, bool isInputType, IGraphType graphType, MemberInfo memberInfo, ref bool ignore)
    {
        if (!isInputType)
            return;
        var parserType = _parserType ?? memberInfo.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (parserType == memberInfo.DeclaringType!)
            bindingFlags |= BindingFlags.NonPublic;
#pragma warning disable IL2075 // UnrecognizedReflectionPattern
        var method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object)], null)
            ?? throw new InvalidOperationException($"Could not find method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' while initializing '{graphType.Name}.{fieldType.Name}'. The method must have a single parameter of type object.");
#pragma warning restore IL2075 // UnrecognizedReflectionPattern
        if (method.ReturnType != typeof(object))
            throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
        fieldType.Parser = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
    }

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument, ParameterInfo parameterInfo)
    {
        var parserType = _parserType ?? parameterInfo.Member.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (parserType == parameterInfo.Member.DeclaringType)
            bindingFlags |= BindingFlags.NonPublic;
#pragma warning disable IL2075 // UnrecognizedReflectionPattern
        var method = parserType.GetMethod(_parserMethodName, bindingFlags, null, [typeof(object)], null)
            ?? throw new InvalidOperationException($"Could not find method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' while initializing argument '{queryArgument.Name}'. The method must have a single parameter of type object.");
#pragma warning restore IL2075 // UnrecognizedReflectionPattern
        if (method.ReturnType != typeof(object))
            throw new InvalidOperationException($"Method '{_parserMethodName}' on CLR type '{parserType.GetFriendlyName()}' must have a return type of object.");
        queryArgument.Parser = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
    }
}
