using System;
using System.Reflection;
using GraphQL.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.NewtonsoftJson
{
    public class ExecutionResultContractResolver : DefaultContractResolver
    {
        private readonly CamelCaseNamingStrategy _camelCase = new CamelCaseNamingStrategy();
        private readonly IErrorParser _errorParser;

        public ExecutionResultContractResolver(IErrorParser errorParser)
        {
            _errorParser = errorParser;
        }

        protected override JsonConverter ResolveContractConverter(Type objectType) =>
            typeof(ExecutionResult).IsAssignableFrom(objectType)
                ? new ExecutionResultJsonConverter(_errorParser)
                : base.ResolveContractConverter(objectType);

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
