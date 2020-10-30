using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    //perhaps this should apply to ReturnValue rather than Method
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class GraphTypeAttribute : Attribute
    {
        public GraphTypeAttribute(Type graphType)
        {
            Type = graphType;
        }

        public GraphTypeAttribute(IGraphType graphType)
        {
            ResolvedType = graphType;
        }

        public Type Type { get; private set; }
        public IGraphType ResolvedType { get; private set; }
    }
}
