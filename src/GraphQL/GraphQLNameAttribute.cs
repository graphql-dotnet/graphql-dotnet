using System;

namespace GraphQL
{
    public class GraphQLNameAttribute : Attribute
    {
        public GraphQLNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}