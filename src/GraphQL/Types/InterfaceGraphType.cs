using GraphQL.Execution;

namespace GraphQL.Types
{
    public class InterfaceGraphType : GraphQLAbstractType
    {
        public override bool IsPossibleType(ExecutionContext context, GraphType type)
        {
            var hasInterfaces = type as IImplementInterfaces;
            if (hasInterfaces != null)
            {
                var interfaces = context.Schema.FindTypes(hasInterfaces.Interfaces);
                return interfaces.Any(i => i.Equals(this));
            }

            return false;
        }
    }
}
