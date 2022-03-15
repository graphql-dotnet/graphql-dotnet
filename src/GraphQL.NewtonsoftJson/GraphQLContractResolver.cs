using System.Reflection;
using GraphQL.Execution;
using GraphQL.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// An <see cref="IContractResolver"/> for GraphQL.NET.
    /// </summary>
    public class GraphQLContractResolver : DefaultContractResolver
    {
        private readonly CamelCaseNamingStrategy _camelCase = new CamelCaseNamingStrategy();
        private readonly IErrorInfoProvider _errorInfoProvider;

        /// <summary>
        /// Initializes an instance with the specified <see cref="IErrorInfoProvider"/>.
        /// </summary>
        public GraphQLContractResolver(IErrorInfoProvider errorInfoProvider)
        {
            _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
        }

        /// <inheritdoc/>
        protected override JsonConverter? ResolveContractConverter(Type objectType)
        {
            if (typeof(ExecutionResult).IsAssignableFrom(objectType))
                return new ExecutionResultJsonConverter(NamingStrategy);

            if (typeof(ExecutionError).IsAssignableFrom(objectType))
                return new ExecutionErrorJsonConverter(_errorInfoProvider);

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

        /// <inheritdoc/>
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
