using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// <para>
    /// A field resolver returns an object for a given field within a graph.
    /// </para><para>
    /// The <see cref="FieldType.Resolver"/> property defines the field resolver to be used for the field.
    /// </para><para>
    /// Typically an instance of <see cref="FuncFieldResolver{TSourceType, TReturnType}">FuncFieldResolver</see> or
    /// <see cref="AsyncFieldResolver{TSourceType, TReturnType}">AsyncFieldResolver</see> is created when code needs
    /// to execute within the field resolver - typically by calling
    /// <see cref="Builders.FieldBuilder{TSourceType, TReturnType}">FieldBuilder</see>.<see cref="Builders.FieldBuilder{TSourceType, TReturnType}.Resolve(System.Func{IResolveFieldContext{TSourceType}, TReturnType})">Resolve</see>
    /// or <see cref="Builders.FieldBuilder{TSourceType, TReturnType}">FieldBuilder</see>.<see cref="Builders.FieldBuilder{TSourceType, TReturnType}.ResolveAsync(System.Func{IResolveFieldContext{TSourceType}, System.Threading.Tasks.Task{TReturnType}})">ResolveAsync</see>.
    /// </para><para>
    /// When mapping fields to source object properties via
    /// <see cref="ComplexGraphType{TSourceType}.Field{TProperty}(System.Linq.Expressions.Expression{System.Func{TSourceType, TProperty}}, bool, System.Type)">Field(x => x.Name)</see>,
    /// <see cref="ExpressionFieldResolver{TSourceType, TProperty}">ExpressionFieldResolver</see> is used.
    /// </para><para>
    /// When a field resolver is not defined, such as with <see cref="ComplexGraphType{TSourceType}.Field{TGraphType, TReturnType}(string)">Field("Name")</see>,
    /// the static instance of <see cref="NameFieldResolver"/> is used.
    /// </para>
    /// </summary>
    public interface IFieldResolver
    {
        /// <summary>
        /// Returns an object, <see langword="null"/>, or a <see cref="Task{TResult}"/> for the specified field. If a task is returned, then this task will be awaited to obtain the actual object.
        /// </summary>
        ValueTask<object?> ResolveAsync(IResolveFieldContext context);
    }
}
