using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public abstract class SchemaDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public string Name { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        public TType GetArgument<TType>(string name, TType defaultValue = default)
        {
            return (TType)GetArgument(typeof(TType), name, defaultValue);
        }

        public object GetArgument(Type argumentType, string name, object defaultValue = null)
        {
            if (Arguments == null || !Arguments.TryGetValue(name, out var arg))
            {
                return defaultValue;
            }

            if (arg is Dictionary<string, object> inputObject)
            {
                var type = argumentType;
                if (type.Namespace?.StartsWith("System") == true)
                {
                    return arg;
                }

                return inputObject.ToObject(type);
            }

            return arg.GetPropertyValue(argumentType);
        }

        public override void VisitSchema(Schema schema)
        {
            base.VisitSchema(schema);
            schema.SetDirective(Name, this);
        }

        public override void VisitScalar(ScalarGraphType scalar)
        {
            base.VisitScalar(scalar);
            scalar.SetDirective(Name, this);
        }

        public override void VisitObject(ObjectGraphType type)
        {
            base.VisitObject(type);
            type.SetDirective(Name, this);
        }

        public override void VisitObject(IObjectGraphType type)
        {
            base.VisitObject(type);
            type.SetDirective(Name, this);
        }

        public override void VisitField(FieldType field)
        {
            base.VisitField(field);
            field.SetDirective(Name, this);
        }

        public override void VisitArgument(QueryArgument argument)
        {
            base.VisitArgument(argument);
            argument.SetDirective(Name, this);
        }

        public override void VisitInterface(InterfaceGraphType interfaceDefinition)
        {
            base.VisitInterface(interfaceDefinition);
            interfaceDefinition.SetDirective(Name, this);
        }

        public override void VisitUnion(UnionGraphType union)
        {
            base.VisitUnion(union);
            union.SetDirective(Name, this);
        }

        public override void VisitEnumeration(EnumerationGraphType type)
        {
            base.VisitEnumeration(type);
            type.SetDirective(Name, this);
        }

        public override void VisitEnumerationValue(EnumValueDefinition value)
        {
            base.VisitEnumerationValue(value);
            value.SetDirective(Name, this);
        }

        public override void VisitInputObject(InputObjectGraphType type)
        {
            base.VisitInputObject(type);
            type.SetDirective(Name, this);
        }

        public override void VisitInputField(FieldType value)
        {
            base.VisitInputField(value);
            value.SetDirective(Name, this);
        }
    }
}
