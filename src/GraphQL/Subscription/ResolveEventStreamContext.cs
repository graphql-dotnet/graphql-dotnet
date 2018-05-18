using GraphQL.Types;

namespace GraphQL.Subscription
{
    public class ResolveEventStreamContext : ResolveFieldContext
    {
        public ResolveEventStreamContext()
        {
            
        }

        public ResolveEventStreamContext(ResolveEventStreamContext context)
        {
            SourceObject = context.SourceObject;
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

    public class ResolveEventStreamContext<TSourceType> : ResolveEventStreamContext
    {
        public ResolveEventStreamContext(ResolveEventStreamContext context): base(context)
        {
            
        }

        public TSourceType Source => (TSourceType) SourceObject;
    }
}
