using GraphQL.Execution;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Utilities
{
    public static class ExecutionContextExtensions
    {
        public static ResolveFieldContext CreateResolveFieldContext(this ExecutionContext context, ExecutionNode node)
        {
            var arguments = ExecutionHelper.GetArgumentValues(context.Schema, node.FieldDefinition.Arguments, node.Field.Arguments, context.Variables);
            var subFields = ExecutionHelper.SubFieldsFor(context, node.FieldDefinition.ResolvedType, node.Field);

            return new ResolveFieldContext
            {
                FieldName = node.Field.Name,
                FieldAst = node.Field,
                FieldDefinition = node.FieldDefinition,
                ReturnType = node.FieldDefinition.ResolvedType,
                ParentType = node.GetParentType(context.Schema),
                Arguments = arguments,
                Source = node.Source,
                Schema = context.Schema,
                Document = context.Document,
                Fragments = context.Fragments,
                RootValue = context.RootValue,
                UserContext = context.UserContext,
                Operation = context.Operation,
                Variables = context.Variables,
                CancellationToken = context.CancellationToken,
                Metrics = context.Metrics,
                Errors = context.Errors,
                Path = node.Path,
                SubFields = subFields
            };
        }
    }
}
