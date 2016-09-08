using GraphQL.SchemaGenerator.Helpers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Wrappers
{
    public class KeyValuePairInputGraphType<TKey, TValue> : InputObjectGraphType
        where TKey : GraphType
        where TValue : GraphType
    {
        public KeyValuePairInputGraphType()
        {
            Name = "Input_KeyValuePair_" + TypeHelper.GetFullName(typeof(TKey)) + "_" + TypeHelper.GetFullName(typeof(TValue));

            Field<TKey>("key");
            Field<TValue>("value");
        }
    }
}
