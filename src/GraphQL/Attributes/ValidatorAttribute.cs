using System.Reflection;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies a custom validator method for a field argument or a field of an input object in a GraphQL schema.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class ValidatorAttribute : GraphQLAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type? _validatorType;
    private readonly string _validatorMethodName;
    private readonly bool _includePrivate;

    /// <summary>
    /// Specifies a custom validator method for a field of an input object in a GraphQL schema using the specified validator method name.
    /// The method must exist on the declaring type of the field and be declared as static.
    /// The method may be private or public and must have the signature 'void <paramref name="validatorMethodName"/>(object value)'.
    /// If it is public, you may wish to mark the method with <see cref="IgnoreAttribute"/> to prevent it from being exposed in the schema.
    /// </summary>
    [RequiresUnreferencedCode("Please ensure the specified method is not trimmed or use an alternative constructor.")]
    public ValidatorAttribute(string validatorMethodName)
    {
        _validatorMethodName = validatorMethodName
            ?? throw new ArgumentNullException(nameof(validatorMethodName));
        _includePrivate = true;
    }

    /// <summary>
    /// Specifies a custom validator method for a field of an input object in a GraphQL schema using the 'Validator' method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'void Validator(object value)'.
    /// </summary>
    public ValidatorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type validatorType)
    {
        _validatorType = validatorType
            ?? throw new ArgumentNullException(nameof(validatorType));
        _validatorMethodName = nameof(FieldType.Validator);
    }

    /// <summary>
    /// Specifies a custom validator method for a field of an input object in a GraphQL schema using the specified method method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'void <paramref name="validatorMethodName"/>(object value)'.
    /// </summary>
    public ValidatorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type validatorType, string validatorMethodName)
    {
        _validatorType = validatorType ?? throw new ArgumentNullException(nameof(validatorType));
        _validatorMethodName = validatorMethodName ?? throw new ArgumentNullException(nameof(validatorMethodName));
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType, IGraphType graphType, MemberInfo memberInfo, ref bool ignore)
    {
        if (!isInputType)
            return;
        var validatorType = _validatorType ?? memberInfo.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (_includePrivate)
            bindingFlags |= BindingFlags.NonPublic;
#pragma warning disable IL2075 // UnrecognizedReflectionPattern
        var method = validatorType.GetMethod(_validatorMethodName, bindingFlags, null, [typeof(object)], null)
            ?? throw new InvalidOperationException($"Could not find method '{_validatorMethodName}' on CLR type '{validatorType.GetFriendlyName()}' while initializing '{graphType.Name}.{fieldType.Name}'. The method must have a single parameter of type object.");
#pragma warning restore IL2075 // UnrecognizedReflectionPattern
        if (method.ReturnType != typeof(void))
            throw new InvalidOperationException($"Method '{_validatorMethodName}' on CLR type '{validatorType.GetFriendlyName()}' must have a void return type.");
        fieldType.Validator += (Action<object>)method.CreateDelegate(typeof(Action<object>));
    }

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument, ParameterInfo parameterInfo)
    {
        var validatorType = _validatorType ?? parameterInfo.Member.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (_includePrivate)
            bindingFlags |= BindingFlags.NonPublic;
#pragma warning disable IL2075 // UnrecognizedReflectionPattern
        var method = validatorType.GetMethod(_validatorMethodName, bindingFlags, null, [typeof(object)], null)
            ?? throw new InvalidOperationException($"Could not find method '{_validatorMethodName}' on CLR type '{validatorType.GetFriendlyName()}' while initializing argument '{queryArgument.Name}'. The method must have a single parameter of type object.");
#pragma warning restore IL2075 // UnrecognizedReflectionPattern
        if (method.ReturnType != typeof(void))
            throw new InvalidOperationException($"Method '{_validatorMethodName}' on CLR type '{validatorType.GetFriendlyName()}' must have a void return type.");
        queryArgument.Validator += (Action<object>)method.CreateDelegate(typeof(Action<object>));
    }
}
