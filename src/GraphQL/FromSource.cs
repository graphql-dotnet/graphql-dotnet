using GraphQL.Types;

namespace GraphQL
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromSourceAttribute : GraphQLAttribute
    {
        public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
            if (!argumentInformation.ParameterInfo.ParameterType.IsAssignableFrom(argumentInformation.SourceType))
                throw new InvalidOperationException($"Source parameter type '{argumentInformation.ParameterInfo.ParameterType.Name}' does not match source type of '{argumentInformation.SourceType.Name}'.");
            argumentInformation.SetDelegate(context => (TParameterType)context.Source!);
        }
    }
}
