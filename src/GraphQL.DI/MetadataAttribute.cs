using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public class MetadataAttribute : Attribute
    {
        public MetadataAttribute(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public object Value { get; set; }
    }
}
