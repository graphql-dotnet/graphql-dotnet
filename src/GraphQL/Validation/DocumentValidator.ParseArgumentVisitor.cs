using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation;

public partial class DocumentValidator
{
    private class ParseArgumentVisitor : ASTVisitor<ParseArgumentVisitor.Context>
    {
        public static readonly ParseArgumentVisitor Instance = new();

        private ParseArgumentVisitor() { }

        protected override async ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, Context context)
        {
            var type = operationDefinition.Operation switch
            {
                OperationType.Query => context.Schema.Query,
                OperationType.Mutation => context.Schema.Mutation,
                OperationType.Subscription => context.Schema.Subscription,
                _ => null
            };
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
                if (context.Schema.AllTypes[typeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
                {
                    context.Types.Push(type);
                    await base.VisitInlineFragmentAsync(inlineFragment, context).ConfigureAwait(false);
                    context.Types.Pop();
                }
            }
        }

        protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, Context context)
        {
            if (context.Schema.AllTypes[fragmentDefinition.TypeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
            {
                context.Types.Push(type);
                await base.VisitFragmentDefinitionAsync(fragmentDefinition, context).ConfigureAwait(false);
                context.Types.Pop();
            }
        }

        protected override async ValueTask VisitFieldAsync(GraphQLField field, Context context)
        {
            var fieldType = field.Name.Value == context.Schema.TypeMetaFieldType.Name
                ? context.Schema.TypeMetaFieldType
                : context.Type?.Fields.Find(field.Name.Value);
            if (fieldType == null)
                return;
            if (field.Arguments?.Count > 0)
            {
                try
                {
                    var arguments = ExecutionHelper.GetArguments(fieldType.Arguments, field.Arguments, context.Variables);
                    if (arguments != null)
                    {
                        (context.ArgumentValues ??= new()).Add(field, arguments);
                    }
                }
                catch (Exception ex)
                {
                    // todo: report error properly
                    context.ValidationContext.ReportError(new ValidationError($"Error trying to resolve field '{field.Name.Value}'.", ex));
                }
            }
            if (field.Directives?.Count > 0)
            {
                try
                {
                    var directives = ExecutionHelper.GetDirectives(field, context.Variables, context.Schema);
                    if (directives != null)
                    {
                        (context.DirectiveValues ??= new()).Add(field, directives);
                    }
                }
                catch (Exception ex)
                {
                    // todo: report error properly
                    context.ValidationContext.ReportError(new ValidationError($"Error trying to resolve field '{field.Name.Value}'.", ex));
                }
            }
            if (fieldType.ResolvedType?.GetNamedType() is IComplexGraphType type)
            {
                context.Types.Push(type);
                await base.VisitFieldAsync(field, context).ConfigureAwait(false);
                context.Types.Pop();
            }
        }

        public class Context : IASTVisitorContext, IDisposable
        {
            public ISchema Schema => ValidationContext.Schema;
            public ValidationContext ValidationContext { get; set; }
            public Stack<IComplexGraphType> Types { get; set; }
            public IComplexGraphType? Type => Types.Count > 0 ? Types.Peek() : null;
            public Variables Variables { get; set; }
            public CancellationToken CancellationToken => ValidationContext.CancellationToken;
            public Dictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues { get; set; }
            public Dictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? DirectiveValues { get; set; }

            private static Stack<IComplexGraphType>? _reusableTypes;

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
