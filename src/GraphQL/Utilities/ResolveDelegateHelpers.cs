using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Provides helper methods for AOT-compatible interceptors of
/// <see cref="Builders.FieldBuilder{TSourceType, TReturnType}.ResolveDelegate(Delegate?)"/>.
/// </summary>
public static class ResolveDelegateHelpers
{
    /// <summary>
    /// Returns a resolver function for a method parameter.
    /// If the parameter has a <see cref="ParameterAttribute"/>, its
    /// <see cref="ParameterAttribute.GetResolver{T}(ArgumentInformation)"/> is called.
    /// For <see cref="IResolveFieldContext"/> and <see cref="System.Threading.CancellationToken"/>
    /// parameters, built-in resolvers are returned.
    /// Otherwise, the argument is resolved from the field context via
    /// <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>
    /// using the parameter name. Arguments are assumed to have already been added to the field.
    /// </summary>
    /// <typeparam name="TParameterType">The CLR type of the method parameter.</typeparam>
    /// <param name="fieldType">The field type (used for <see cref="ParameterAttribute"/> resolution).</param>
    /// <param name="parameterInfo">The parameter info for the method parameter.</param>
    /// <returns>A function that resolves the parameter value from an <see cref="IResolveFieldContext"/>.</returns>
    public static Func<IResolveFieldContext, TParameterType> BuildArgument<TParameterType>(
        FieldType fieldType,
        ParameterInfo parameterInfo)
    {
        var typeInformation = new TypeInformation(parameterInfo);
        typeInformation.ApplyAttributes();
        var argumentInfo = new ArgumentInformation(parameterInfo, null, fieldType, typeInformation);
        argumentInfo.ApplyAttributes();

        var resolver = AutoRegisteringHelper.GetParameterResolver<TParameterType>(argumentInfo);
        if (resolver != null)
            return resolver;

        // Fall back to resolving from the GraphQL argument by parameter name.
        // Arguments are assumed to have already been added to the field.
        var name = parameterInfo.Name!;
        return context => context.GetArgument<TParameterType>(name);
    }
}
