using System.Reflection;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL;

/// <summary>
/// Specifies a custom argument validation method for a field in a GraphQL schema.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidateArgumentsAttribute : GraphQLAttribute
{
    private const string DEFAULT_METHOD_NAME = "ValidateArguments";

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type? _validationType;
    private readonly string _validationMethodName;

    /// <summary>
    /// Specifies a custom argument validation method for a field in a GraphQL schema using the specified validation method name.
    /// The method must exist on the declaring type of the field and be declared as static.
    /// The method may be private or public and must have the signature 'ValueTask <paramref name="validationMethodName"/>(FieldArgumentsValidationContext context)'.
    /// </summary>
    [RequiresUnreferencedCode("Please ensure the specified method is not trimmed or use an alternative constructor.")]
    public ValidateArgumentsAttribute(string validationMethodName)
    {
        _validationMethodName = validationMethodName
            ?? throw new ArgumentNullException(nameof(validationMethodName));
    }

    /// <summary>
    /// Specifies a custom argument validation method for a field in a GraphQL schema using the 'ValidateArguments' method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'ValueTask ValidateArguments(FieldArgumentsValidationContext context)'.
    /// </summary>
    public ValidateArgumentsAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type validationType)
    {
        _validationType = validationType
            ?? throw new ArgumentNullException(nameof(validationType));
        _validationMethodName = DEFAULT_METHOD_NAME;
    }

    /// <summary>
    /// Specifies a custom argument validation method for a field in a GraphQL schema using the specified method from a specified type.
    /// The method must exist on the specified type and must be static and public.
    /// The method must have the signature 'ValueTask <paramref name="validationMethodName"/>(FieldArgumentsValidationContext context)'.
    /// </summary>
    public ValidateArgumentsAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type validationType, string validationMethodName)
    {
        _validationType = validationType ?? throw new ArgumentNullException(nameof(validationType));
        _validationMethodName = validationMethodName ?? throw new ArgumentNullException(nameof(validationMethodName));
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType, IGraphType graphType, MemberInfo memberInfo, ref bool ignore)
    {
        if (isInputType)
            return;
        var validationType = _validationType ?? memberInfo.DeclaringType!;
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (validationType == memberInfo.DeclaringType!)
            bindingFlags |= BindingFlags.NonPublic;
#pragma warning disable IL2075 // UnrecognizedReflectionPattern
        var method = validationType.GetMethod(_validationMethodName, bindingFlags, null, [typeof(FieldArgumentsValidationContext)], null)
            ?? throw new InvalidOperationException($"Could not find method '{_validationMethodName}' on CLR type '{validationType.GetFriendlyName()}' while initializing '{graphType.Name}.{fieldType.Name}'. The method must have a single parameter of type {nameof(FieldArgumentsValidationContext)}.");
#pragma warning restore IL2075 // UnrecognizedReflectionPattern
        if (method.ReturnType != typeof(ValueTask))
            throw new InvalidOperationException($"Method '{_validationMethodName}' on CLR type '{validationType.GetFriendlyName()}' must have a return type of ValueTask.");
        fieldType.ValidateArguments = (Func<FieldArgumentsValidationContext, ValueTask>)method.CreateDelegate(typeof(Func<FieldArgumentsValidationContext, ValueTask>));
    }
}
