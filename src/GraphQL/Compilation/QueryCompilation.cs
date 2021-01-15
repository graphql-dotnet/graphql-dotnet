using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Compilation
{
    public class QueryCompilation
    {
        public static CompiledNode Compile(ISchema schema, Document document, Variables variables, Operation operation)
        {
            var rootOperationType = GetOperationRootType(document, schema, operation);
            var fields = CollectFieldsRecursive(schema, document, variables, operation.SelectionSet, rootOperationType);
            var rootNode = new CompiledNode(rootOperationType, fields);
            return rootNode;
        }

        private static Dictionary<string, CompiledField> CollectFieldsRecursive(
            ISchema schema,
            Document document,
            Variables variables,
            SelectionSet selectionSet,
            IObjectGraphType rootOperationType)
        {
            var fields = new Dictionary<string, CompiledField>();
            var collected = CollectFields(schema, variables, document, rootOperationType, selectionSet);
            foreach (var kv in collected)
            {
                var field = CompileFieldRecursive(schema, document, variables, rootOperationType, kv.Value);
                fields.Add(kv.Key, field);
            }

            return fields;
        }

        private static CompiledField CompileFieldRecursive(ISchema schema, Document document, Variables variables, IObjectGraphType graphType, Field value)
        {
            var definition = GetFieldDefinition(schema, graphType, value);
            var resolve = GetResolve(schema, document, variables, definition, value);

            return new CompiledField(definition, value, resolve);
        }

        private static Func<object, bool, CompiledNode> GetResolve(ISchema schema, Document document, Variables variables, FieldType definition, Field field)
        {
            var unwrapped = definition?.ResolvedType;
            while (unwrapped is IProvideResolvedType provideResolvedType)
            {
                unwrapped = provideResolvedType.ResolvedType;
            }
            switch (unwrapped)
            {
                case IAbstractGraphType abstractType:
                {
                    var gqlToNode = new Dictionary<IObjectGraphType, CompiledNode>();
                    foreach (var possilbe in abstractType.PossibleTypes)
                    {
                        var fields = CollectFieldsRecursive(schema, document, variables, field.SelectionSet, possilbe);
                        var rootNode = new CompiledNode(possilbe, fields);
                        gqlToNode.Add(possilbe, rootNode);
                    }
                    var defaultType = new CompiledNode(abstractType, new Dictionary<string, CompiledField>());

                    return (value, isResolved) =>
                    {
                        var objType = abstractType.GetObjectType(value, schema);
                        return objType is { } && gqlToNode.TryGetValue(objType, out var foundType)
                            ? foundType
                            : defaultType;
                    };
                }
                case IObjectGraphType objectType:
                {
                    var fields = CollectFieldsRecursive(schema, document, variables, field.SelectionSet, objectType);
                    var rootNode = new CompiledNode(objectType, fields);

                    return (value, isResolved) => rootNode;
                }
                case ScalarGraphType scalar:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns the root graph type for the execution -- for a specified schema and operation type.
        /// </summary>
        public static IObjectGraphType GetOperationRootType(Document document, ISchema schema, Operation operation)
        {
            IObjectGraphType type;

            switch (operation.OperationType)
            {
                case OperationType.Query:
                    type = schema.Query;
                    break;

                case OperationType.Mutation:
                    type = schema.Mutation;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for mutations").AddLocation(operation, document);
                    break;

                case OperationType.Subscription:
                    type = schema.Subscription;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for subscriptions").AddLocation(operation, document);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), "Can only execute queries, mutations and subscriptions.");
            }

            return type;
        }

        /// <summary>
        /// Returns a <see cref="FieldType"/> for the specified AST <see cref="Field"/> within a specified parent
        /// output graph type within a given schema. For meta-fields, returns the proper meta-field field type.
        /// </summary>
        public static FieldType GetFieldDefinition(ISchema schema, IObjectGraphType parentType, Field field)
        {
            if (field.Name == schema.SchemaMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.SchemaMetaFieldType;
            }
            if (field.Name == schema.TypeMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.TypeMetaFieldType;
            }
            if (field.Name == schema.TypeNameMetaFieldType.Name)
            {
                return schema.TypeNameMetaFieldType;
            }

            if (parentType == null)
            {
                throw new ArgumentNullException(nameof(parentType), $"Schema is not configured correctly to fetch field '{field.Name}'. Are you missing a root type?");
            }

            return parentType.GetField(field.Name);
        }

        private static Fields CollectFields(
            ISchema schema,
            Variables variables,
            Document document,
            IGraphType specificType,
            SelectionSet selectionSet,
            Fields fields,
            List<string> visitedFragmentNames)
        {
            if (selectionSet != null)
            {
                foreach (var selection in selectionSet.SelectionsList)
                {
                    if (selection is Field field)
                    {
                        if (!ShouldIncludeNode(schema, variables, field.Directives))
                        {
                            continue;
                        }

                        fields.Add(field);
                    }
                    else if (selection is FragmentSpread spread)
                    {
                        if (visitedFragmentNames.Contains(spread.Name)
                            || !ShouldIncludeNode(schema, variables, spread.Directives))
                        {
                            continue;
                        }

                        visitedFragmentNames.Add(spread.Name);

                        var fragment = document.Fragments.FindDefinition(spread.Name);
                        if (fragment == null
                            || !ShouldIncludeNode(schema, variables, fragment.Directives)
                            || !DoesFragmentConditionMatch(schema, fragment.Type.Name, specificType))
                        {
                            continue;
                        }

                        CollectFields(schema, variables, document, specificType, fragment.SelectionSet, fields, visitedFragmentNames);
                    }
                    else if (selection is InlineFragment inline)
                    {
                        var name = inline.Type != null ? inline.Type.Name : specificType.Name;

                        if (!ShouldIncludeNode(schema, variables, inline.Directives)
                          || !DoesFragmentConditionMatch(schema, name, specificType))
                        {
                            continue;
                        }

                        CollectFields(schema, variables, document, specificType, inline.SelectionSet, fields, visitedFragmentNames);
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Before execution, the selection set is converted to a grouped field set by calling CollectFields().
        /// Each entry in the grouped field set is a list of fields that share a response key (the alias if defined,
        /// otherwise the field name). This ensures all fields with the same response key included via referenced
        /// fragments are executed at the same time.
        /// <br/><br/>
        /// See http://spec.graphql.org/June2018/#sec-Field-Collection and http://spec.graphql.org/June2018/#CollectFields()
        /// </summary>
        public static Dictionary<string, Field> CollectFields(
            ISchema schema,
            Variables variables,
            Document document,
            IGraphType specificType,
            SelectionSet selectionSet)
        {
            return CollectFields(schema, variables, document, specificType, selectionSet, Fields.Empty(), new List<string>());
        }

        /// <summary>
        /// Examines @skip and @include directives for a node and returns a value indicating if the node should be included or not.
        /// <br/><br/>
        /// Note: Neither @skip nor @include has precedence over the other. In the case that both the @skip and @include
        /// directives are provided on the same field or fragment, it must be queried only if the @skip condition
        /// is false and the @include condition is true. Stated conversely, the field or fragment must not be queried
        /// if either the @skip condition is true or the @include condition is false.
        /// </summary>
        public static bool ShouldIncludeNode(
            ISchema schema,
            Variables variables,
            Directives directives)
        {
            if (directives != null)
            {
                var directive = directives.Find(DirectiveGraphType.Skip.Name);
                if (directive != null)
                {
                    var values = ExecutionHelper.GetArgumentValues(
                        schema,
                        DirectiveGraphType.Skip.Arguments,
                        directive.Arguments,
                        variables);

                    if (values.TryGetValue("if", out object ifObj) && bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal)
                        return false;
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = ExecutionHelper.GetArgumentValues(
                        schema,
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        variables);

                    return values.TryGetValue("if", out object ifObj) && bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal;
                }
            }

            return true;
        }

        /// <summary>
        /// This method calculates the criterion for matching fragment definition (spread or inline) to a given graph type.
        /// This criterion determines the need to fill the resulting selection set with fields from such a fragment.
        /// <br/><br/>
        /// See http://spec.graphql.org/June2018/#DoesFragmentTypeApply()
        /// </summary>
        public static bool DoesFragmentConditionMatch(ISchema schema, string fragmentName, IGraphType type)
        {
            if (string.IsNullOrWhiteSpace(fragmentName))
            {
                return true;
            }

            var conditionalType = schema.FindType(fragmentName);

            if (conditionalType == null)
            {
                return false;
            }

            if (conditionalType.Equals(type))
            {
                return true;
            }

            if (conditionalType is IAbstractGraphType abstractType)
            {
                return abstractType.IsPossibleType(type);
            }

            return false;
        }
    }
}
