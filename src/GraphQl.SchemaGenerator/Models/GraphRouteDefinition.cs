using System;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQl.SchemaGenerator.Models
{
    public class GraphRouteDefinition
    {
        public bool IsMutation { get; set; }
        public string Name { get; }

        public string MethodName { get; }

        public Type ResponseType { get; }

        public IEnumerable<ParameterInfo> Parameters { get; }

        public Type ControllerType { get; internal set; }

        public GraphRouteDefinition(Type controllerType, 
            string name, 
            string methodName, 
            Type returnType, 
            IEnumerable<ParameterInfo> parameters)
        {
            ControllerType = controllerType;
            Name = name;
            MethodName = methodName;
            Parameters = parameters;
            ResponseType = returnType;
        }
    }
}
