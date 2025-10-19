using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Utilities;

/// <summary>
/// Removes private types and fields from a schema.
/// </summary>
public sealed class RemovePrivateTypesAndFieldsVisitor : BaseSchemaNodeVisitor
{
    /// <summary>
    /// A singleton instance of the <see cref="RemovePrivateTypesAndFieldsVisitor"/> class,
    /// which can be used to visit a schema and remove private types and fields.
    /// </summary>
    public static readonly RemovePrivateTypesAndFieldsVisitor Instance = new();

    private RemovePrivateTypesAndFieldsVisitor()
    {
    }

    /// <inheritdoc/>
    public override void VisitSchema(ISchema schema)
    {
        if (schema.Query?.IsPrivate == true)
            throw new InvalidOperationException("Schema's Query type must not be a private type.");
        if (schema.Mutation?.IsPrivate == true)
            schema.Mutation = null;
        if (schema.Subscription?.IsPrivate == true)
            schema.Subscription = null;

        List<ROM>? typesToRemove = null;
        foreach (var pair in schema.AllTypes.Dictionary)
        {
            if (pair.Value.IsPrivate)
                (typesToRemove ??= []).Add(pair.Key);
        }
        if (typesToRemove != null)
        {
            foreach (var key in typesToRemove)
                schema.AllTypes.Dictionary.Remove(key);
        }
    }

    /// <inheritdoc/>
    public override void VisitInputObject(IInputObjectGraphType type, ISchema schema) => ExamineFields(type, type.Fields);

    /// <inheritdoc/>
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        ExamineFields(type, type.Fields);
        var interfaceList = type.ResolvedInterfaces.List;
        for (int i = interfaceList.Count - 1; i >= 0; i--)
        {
            if (interfaceList[i].IsPrivate)
                interfaceList.RemoveAt(i);
        }
    }

    /// <inheritdoc/>
    public override void VisitInterface(IInterfaceGraphType type, ISchema schema)
    {
        ExamineFields(type, type.Fields);
        // todo: remove inherited interfaces that are private (interface inheritance not currently supported by GraphQL.NET)
        RemovePrivateTypes(type.PossibleTypes);
    }

    /// <inheritdoc/>
    public override void VisitUnion(UnionGraphType type, ISchema schema) => RemovePrivateTypes(type.PossibleTypes);

    /// <inheritdoc/>
    public override void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema)
    {
        if (argument.ResolvedType?.IsPrivate == true)
            throw new InvalidOperationException($"Cannot reference the private type '{argument.ResolvedType.Name}' in a public directive argument '@{directive.Name}.{argument.Name}'.");
    }

    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema) => ValidateFieldArgument(type, field, argument);

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => ValidateFieldArgument(type, field, argument);

    private static void ValidateFieldArgument(IGraphType parentType, FieldType field, QueryArgument argument)
    {
        if (argument.ResolvedType?.IsPrivate == true)
            throw new InvalidOperationException($"Cannot reference the private type '{argument.ResolvedType.Name}' in a public field argument '{parentType.Name}.{field.Name}.{argument.Name}'.");
    }

    private static void RemovePrivateTypes(PossibleTypes possibleTypes)
    {
        var list = possibleTypes.List;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].IsPrivate)
                list.RemoveAt(i);
        }
    }

    private static void ExamineFields(IGraphType parentType, TypeFields typeFields)
    {
        var fields = typeFields.List;
        for (int i = fields.Count - 1; i >= 0; i--)
        {
            var field = fields[i];
            if (field.IsPrivate)
                fields.RemoveAt(i);
            else if (field.ResolvedType?.IsPrivate == true)
                throw new InvalidOperationException($"Cannot reference the private type '{field.ResolvedType.Name}' in a public field '{parentType.Name}.{field.Name}'.");
        }
    }
}
