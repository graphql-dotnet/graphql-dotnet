using System.Reflection;
using GraphQL.Execution;
using GraphQL.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.NewtonsoftJson
{
    public class GraphQLContractResolver : DefaultContractResolver
    {
        private readonly CamelCaseNamingStrategy _camelCase = new CamelCaseNamingStrategy();
        private readonly IErrorInfoProvider _errorInfoProvider;

        public GraphQLContractResolver(IErrorInfoProvider errorInfoProvider)
        {
            _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
        }

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(ExecutionResult).IsAssignableFrom(objectType))
                return new ExecutionResultJsonConverter(_errorInfoProvider, NamingStrategy);

            if (objectType == typeof(Inputs))
                return new InputsJsonConverter();

            if (objectType == typeof(GraphQLRequest))
                return new GraphQLRequestJsonConverter();

            if (GraphQLRequestListJsonConverter.CanConvertType(objectType))
                return new GraphQLRequestListJsonConverter();

            if (objectType == typeof(OperationMessage))
                return new OperationMessageJsonConverter();

            return base.ResolveContractConverter(objectType);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(Instrumentation.ApolloTrace) || property.DeclaringType?.DeclaringType == typeof(Instrumentation.ApolloTrace))
            {
                property.PropertyName = _camelCase.GetPropertyName(member.Name, false);
            }

            return property;
        }
    }
}
