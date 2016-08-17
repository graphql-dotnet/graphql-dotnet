using System;
using System.ComponentModel;
using System.Reflection;
using GraphQL.SchemaGenerator.Helpers;
using GraphQL.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Schema
{
    public class SchemaBuilder
    {
        private Type _interfaceWrapper = typeof(InterfaceGraphTypeWrapper<>);
        private Type _objectWrapper = typeof(ObjectGraphTypeWrapper<>);

        public ISchema BuildSchema(Type schemaType)
        {
            var schema = new GraphQL.Types.Schema();

            var queryProperty = schemaType.GetProperty("Query");
            if (queryProperty != null)
            {
                var graphType = TypeHelper.GetGraphType(queryProperty);
                schema.Query = Activator.CreateInstance(_objectWrapper.MakeGenericType(graphType)) as ObjectGraphType;
            }

            var mutationProperty = schemaType.GetProperty("Mutation");
            if (mutationProperty != null)
            {
                var graphType = TypeHelper.GetGraphType(queryProperty);
                schema.Mutation = Activator.CreateInstance(_objectWrapper.MakeGenericType(graphType)) as ObjectGraphType;
            }

            return schema;
        }

        public void BuildEnum(EnumerationGraphType enumGraphType, Type enumType)
        {
            enumGraphType.Name = TypeHelper.GetDisplayName(enumType) ?? enumType.Name;
            foreach (var e in Enum.GetValues(enumType))
            {
                string name = e.ToString();
                var value = Convert.ChangeType(e, typeof(int));
                string description = $"{name}:{value}";
                var field = enumType.GetField(name);
                if (field != null)
                {
                    var desc = field.GetCustomAttribute<DescriptionAttribute>();
                    if (desc != null)
                    {
                        description = desc.Description;
                    }
                }
                enumGraphType.AddValue(name, description, value);
            }
        }
    }
}
