using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    public static class ResolveFieldContextExtensions
    {
        public static ResolveFieldContext<T> As<T>(this ResolveFieldContext context)
        {
            return new ResolveFieldContext<T>()
            {
                Arguments = context.Arguments,
                CancellationToken = context.CancellationToken,
                Document = context.Document,
                Errors = context.Errors,
                FieldAst = context.FieldAst,
                FieldDefinition = context.FieldDefinition,
                FieldName = context.FieldName,
                Fragments = context.Fragments,
                Metrics = context.Metrics,
                Operation = context.Operation,
                ParentType = context.ParentType,
                Path = context.Path,
                ReturnType = context.ReturnType,
                RootValue = context.RootValue,
                Schema = context.Schema,
                Source = (T)context.Source,
                SubFields = context.SubFields,
                UserContext = context.UserContext,
                Variables = context.Variables
            };
        }
    }
}
