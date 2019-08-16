using System;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class FederatedSchema
    {
        public static ISchema For(string[] typeDefinitions, Action<FederatedSchemaBuilder> configure = null)
        {
            var defs = string.Join("\n", typeDefinitions);
            return For(defs, configure);
        }

        public static ISchema For(string typeDefinitions, Action<FederatedSchemaBuilder> configure = null)
        {
            var builder = new FederatedSchemaBuilder();
            configure?.Invoke(builder);
            return builder.Build(typeDefinitions);
        }
    }
}
