using GraphQL.Types;

namespace GraphQL
{
    internal class HardCodedExampleAttribute : GraphQLAttribute
    {
        public override void Modify(ArgumentInformation argumentInformation)
        {
            // 'int' is inferred, and if it is the improper type for the argument,
            // an exception is thrown immediately within SetExpression
            argumentInformation.SetExpression(x => 33);
        }
    }
}
