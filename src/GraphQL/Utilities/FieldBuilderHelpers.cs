using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Provides helper methods for creating fields from expressions.
/// </summary>
public static class FieldBuilderHelpers
{
    /// <summary>
    /// Creates a field builder from an expression with an optional IFieldResolver.
    /// </summary>
    /// <typeparam name="TSourceType">The source type for the field.</typeparam>
    /// <typeparam name="TProperty">The return type of the field.</typeparam>
    /// <param name="complexGraphType">The complex graph type to add the field to.</param>
    /// <param name="name">The name of the field.</param>
    /// <param name="expression">The property of the source object represented within an expression.</param>
    /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
    /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
    /// <param name="resolver">The field resolver for the field; if <see langword="null"/> then no resolver will be set.</param>
    /// <returns>A field builder for the newly created field.</returns>
    /// <remarks>
    /// When <paramref name="nullable"/> and <paramref name="type"/> are both <see langword="null"/>
    /// the field's nullability depends on <see cref="GlobalSwitches.InferFieldNullabilityFromNRTAnnotations"/> and <paramref name="expression"/> type.
    /// When set to <see langword="true"/> and expression is <see cref="MemberExpression"/>,
    /// the result field nullability will match the Null Reference Type annotations of the member represented by the expression.
    /// If expression is not <see cref="MemberExpression"/> and <typeparamref name="TProperty"/> is of value type
    /// the graph type nullability will match the <typeparamref name="TProperty"/> nullability. Otherwise, the field will be not nullable.
    /// </remarks>
    public static FieldBuilder<TSourceType, TProperty> CreateFieldBuilder<TSourceType, TProperty>(
        IComplexGraphType complexGraphType,
        string name,
        Expression<Func<TSourceType, TProperty>> expression,
        bool? nullable,
        Type? type,
        IFieldResolver? resolver)
    {
        try
        {
            if (type == null && nullable == null && GlobalSwitches.InferFieldNullabilityFromNRTAnnotations)
            {
                if (expression.Body is MemberExpression memberExpression)
                {
                    var typeInfo = AutoRegisteringHelper.GetTypeInformation(memberExpression.Member, complexGraphType is IInputObjectGraphType);
                    type = typeInfo.ConstructGraphType();
                }
                else
                {
                    nullable = typeof(TProperty).IsValueType && Nullable.GetUnderlyingType(typeof(TProperty)) != null;
                    type = typeof(TProperty).GetGraphTypeFromType(nullable.Value, complexGraphType is IInputObjectGraphType);
                }
            }
            else if (type == null)
            {
                nullable ??= false;
                type = typeof(TProperty).GetGraphTypeFromType(nullable.Value, complexGraphType is IInputObjectGraphType);
            }
        }
        catch (ArgumentOutOfRangeException exp)
        {
            throw new ArgumentException($"The GraphQL type for field '{complexGraphType.Name ?? complexGraphType.GetType().Name}.{name}' could not be derived implicitly from expression '{expression}'. " + exp.Message, exp);
        }

        var builder = FieldBuilder<TSourceType, TProperty>.Create(name, type)
            .Description(expression.DescriptionOf())
            .DeprecationReason(expression.DeprecationReasonOf())
            .DefaultValue(expression.DefaultValueOf());

        if (complexGraphType is IInputObjectGraphType)
        {
            builder.ParseValue((value, vc) => value is TProperty ? value : vc.GetPropertyValue(value, typeof(TProperty), builder.FieldType.ResolvedType!)!);
        }

        if (complexGraphType is IObjectGraphType)
        {
            builder.Resolve(resolver);
        }

        if (expression.Body is MemberExpression expr)
        {
            builder.FieldType.Metadata[ComplexGraphType<TSourceType>.ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
        }

        complexGraphType.AddField(builder.FieldType);

        return builder;
    }

    /// <summary>
    /// Creates a field builder from an expression with a Func resolver.
    /// </summary>
    /// <typeparam name="TSourceType">The source type for the field.</typeparam>
    /// <typeparam name="TProperty">The return type of the field.</typeparam>
    /// <param name="complexGraphType">The complex graph type to add the field to.</param>
    /// <param name="name">The name of the field.</param>
    /// <param name="expression">The property of the source object represented within an expression.</param>
    /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
    /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
    /// <param name="resolve">The resolver function for the field.</param>
    /// <returns>A field builder for the newly created field.</returns>
    /// <remarks>
    /// When <paramref name="nullable"/> and <paramref name="type"/> are both <see langword="null"/>
    /// the field's nullability depends on <see cref="GlobalSwitches.InferFieldNullabilityFromNRTAnnotations"/> and <paramref name="expression"/> type.
    /// When set to <see langword="true"/> and expression is <see cref="MemberExpression"/>,
    /// the result field nullability will match the Null Reference Type annotations of the member represented by the expression.
    /// If expression is not <see cref="MemberExpression"/> and <typeparamref name="TProperty"/> is of value type
    /// the graph type nullability will match the <typeparamref name="TProperty"/> nullability. Otherwise, the field will be not nullable.
    /// </remarks>
    public static FieldBuilder<TSourceType, TProperty> CreateFieldFromExpression<TSourceType, TProperty>(
        IComplexGraphType complexGraphType,
        string name,
        Expression<Func<TSourceType, TProperty>> expression,
        bool? nullable,
        Type? type,
        Func<IResolveFieldContext<TSourceType>, TProperty?> resolve)
        => CreateFieldBuilder(complexGraphType, name, expression, nullable, type, new FuncFieldResolverNoAccessor<TSourceType, TProperty>(resolve));
}
