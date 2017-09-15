using GraphQL.Types;

namespace GraphQL.Subscription
{
    public class ResolveEventStreamContext<T> : ResolveFieldContext<T>
    {
        public ResolveEventStreamContext() { }

        public ResolveEventStreamContext(ResolveEventStreamContext context)
        {
            Source = (T)context.Source;
            FieldName = context.FieldName;
            FieldAst = context.FieldAst;
            FieldDefinition = context.FieldDefinition;
            ReturnType = context.ReturnType;
            ParentType = context.ParentType;
            Arguments = context.Arguments;
            Schema = context.Schema;
            Document = context.Document;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Metrics = context.Metrics;
            Errors = context.Errors;
        }
    }

    public class ResolveEventStreamContext : ResolveEventStreamContext<object>
    {
        internal ResolveEventStreamContext<TSourceType> As<TSourceType>()
        {
            return new ResolveEventStreamContext<TSourceType>(this);
        }
    }
}
