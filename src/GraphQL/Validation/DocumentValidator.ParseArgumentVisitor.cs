using System.Collections.Concurrent;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation;

public partial class DocumentValidator
{
    /// <summary>
    /// Visits an AST tree to parse the field and directive argument values.
    /// </summary>
    private class ParseArgumentVisitor : ASTVisitor<ParseArgumentVisitor.Context>
    {
        public static readonly ParseArgumentVisitor Instance = new();

        private ParseArgumentVisitor() { }

        public override ValueTask VisitAsync(ASTNode? node, Context context)
        {
            if (node is IHasDirectivesNode hasDirectivesNode)
            {
                // if any directives were supplied in the document for the field or fragment spread,
                // load all defined arguments for directives on the field or fragment spread
                if (hasDirectivesNode.Directives?.Count > 0)
                {
                    try
                    {
                        var directives = ExecutionHelper.GetDirectives(hasDirectivesNode, context.Variables, context.Schema, context.ValidationContext.Document);
                        if (directives != null)
                        {
                            (context.DirectiveValues ??= new()).TryAdd(node, directives);
                        }
                    }
                    catch (ValidationError ex)
                    {
                        context.ValidationContext.ReportError(ex);
                    }
                }
            }

            return base.VisitAsync(node, context);
        }

        protected override ValueTask VisitDocumentAsync(GraphQLDocument document, Context context)
        {
            // parses the selected operation only
            var selectedOperation = context.ValidationContext.Operation;
            if (selectedOperation != null)
                return VisitAsync(selectedOperation, context);
            // if no operation is selected, nothing is parsed
            return default;
        }

        protected override async ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, Context context)
        {
            // pull the matching graph type for the operation
            var type = operationDefinition.Operation switch
            {
                OperationType.Query => context.Schema.Query,
                OperationType.Mutation => context.Schema.Mutation,
                OperationType.Subscription => context.Schema.Subscription,
                _ => null
            };
            // assuming the schema passed validation, the type should not be null
            if (type == null)
                return;
            context.Types.Push(type);
            await base.VisitOperationDefinitionAsync(operationDefinition, context).ConfigureAwait(false);
            context.Types.Pop();
        }

        protected override async ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, Context context)
        {
            var typeCondition = inlineFragment.TypeCondition;
            if (typeCondition == null)
            {
                await base.VisitInlineFragmentAsync(inlineFragment, context).ConfigureAwait(false);
            }
            else
            {
                // assuming the schema passed validation, we should be find the matching graph type for this inline fragment
                if (context.Schema.AllTypes[typeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
                {
                    context.Types.Push(type);
                    await base.VisitInlineFragmentAsync(inlineFragment, context).ConfigureAwait(false);
                    context.Types.Pop();
                }
            }
        }

        protected override ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, Context context)
        {
            // visit the fragment that this fragment spread references,
            // unless we already visited this fragment spread
            var fragmentName = fragmentSpread.FragmentName.Name.Value;
            if ((context.VisitedFragments ??= []).Add(fragmentName))
            {
                var definition = context.ValidationContext.Document.FindFragmentDefinition(fragmentName);
                if (definition != null)
                {
                    // the fragment name should be one of the definitions in the document, or the
                    // document would not have passed validation
                    return VisitAsync(definition, context);
                }
            }
            return default;
        }

        protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, Context context)
        {
            // the fragment type name should be listed in the document, or the document would have failed validation
            if (context.Schema.AllTypes[fragmentDefinition.TypeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
            {
                context.Types.Push(type);
                await base.VisitFragmentDefinitionAsync(fragmentDefinition, context).ConfigureAwait(false);
                context.Types.Pop();
            }
        }

        protected override async ValueTask VisitFieldAsync(GraphQLField field, Context context)
        {
            // find the field definition for this field
            var schema = context.Schema;
            var fieldDefinition =
                field.Name.Value == schema.TypeMetaFieldType.Name ? schema.TypeMetaFieldType :
                field.Name.Value == schema.TypeNameMetaFieldType.Name ? schema.TypeNameMetaFieldType :
                field.Name.Value == schema.SchemaMetaFieldType.Name ? schema.SchemaMetaFieldType :
                context.Type?.Fields.Find(field.Name.Value);
            // should not be null, or the document would have failed validation
            if (fieldDefinition == null)
                return;
            // if any arguments were supplied in the document for the field, load all defined arguments
            // for the field
            if (field.Arguments?.Count > 0)
            {
                try
                {
                    var arguments = ExecutionHelper.GetArguments(fieldDefinition.Arguments, field.Arguments, context.Variables, context.ValidationContext.Document, field, null, context.Schema.ValueConverter);
                    if (arguments != null)
                    {
                        (context.ArgumentValues ??= new()).TryAdd(field, arguments);
                    }
                }
                catch (ValidationError ex)
                {
                    context.ValidationContext.ReportError(ex);
                }
            }
            // if the field's type is an object or interface, process child fields
            var fieldType = fieldDefinition.ResolvedType?.GetNamedType();
            if (fieldType is IComplexGraphType type)
            {
                context.Types.Push(type);
                await base.VisitFieldAsync(field, context).ConfigureAwait(false);
                context.Types.Pop();
            }
            else if (fieldType is UnionGraphType)
            {
                // union graph types may also contain field spreads (or __typename), but there is no concrete type defined as of yet
                await base.VisitFieldAsync(field, context).ConfigureAwait(false);
            }
        }

        public class Context : IASTVisitorContext, IDisposable
        {
            public ISchema Schema => ValidationContext.Schema;
            public ValidationContext ValidationContext { get; set; }
            public Stack<IComplexGraphType?> Types { get; set; }
            public IComplexGraphType? Type => Types.Count > 0 ? Types.Peek() : null;
            public Variables Variables { get; set; }
            public CancellationToken CancellationToken => ValidationContext.CancellationToken;
            public ConcurrentDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues { get; set; }
            public ConcurrentDictionary<ASTNode, IDictionary<string, DirectiveInfo>>? DirectiveValues { get; set; }
            public HashSet<GraphQLParser.ROM>? VisitedFragments { get; set; }

            private static Stack<IComplexGraphType?>? _reusableTypes;

            public Context(ValidationContext validationContext, Variables variables)
            {
                Types = Interlocked.Exchange(ref _reusableTypes, null) ?? new();
                ValidationContext = validationContext;
                Variables = variables;
            }

            public void Dispose()
            {
                Types.Clear();
                Interlocked.CompareExchange(ref _reusableTypes, Types, null);
                Types = null!;
            }
        }
    }
}
