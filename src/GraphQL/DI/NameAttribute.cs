using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NameAttribute : Attribute
    {
        public NameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
