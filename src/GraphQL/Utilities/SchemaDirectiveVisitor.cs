using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public abstract class SchemaDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public abstract string Name { get; }

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
                if (type.Namespace?.StartsWith("System", StringComparison.InvariantCulture) == true)
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
            schema.SetDirective(this);
        }

        public override void VisitScalar(ScalarGraphType scalar)
        {
            base.VisitScalar(scalar);
            scalar.SetDirective(this);
        }

        public override void VisitObject(IObjectGraphType type)
        {
            base.VisitObject(type);
            type.SetDirective(this);
        }

        public override void VisitFieldDefinition(FieldType field)
        {
            base.VisitFieldDefinition(field);
            field.SetDirective(this);
        }

        public override void VisitArgumentDefinition(QueryArgument argument)
        {
            base.VisitArgumentDefinition(argument);
            argument.SetDirective(this);
        }

        public override void VisitInterface(InterfaceGraphType interfaceDefinition)
        {
            base.VisitInterface(interfaceDefinition);
            interfaceDefinition.SetDirective(this);
        }

        public override void VisitUnion(UnionGraphType union)
        {
            base.VisitUnion(union);
            union.SetDirective(this);
        }

        public override void VisitEnum(EnumerationGraphType type)
        {
            base.VisitEnum(type);
            type.SetDirective(this);
        }

        public override void VisitEnumValue(EnumValueDefinition value)
        {
            base.VisitEnumValue(value);
            value.SetDirective(this);
        }

        public override void VisitInputObject(InputObjectGraphType type)
        {
            base.VisitInputObject(type);
            type.SetDirective(this);
        }

        public override void VisitInputFieldDefinition(FieldType value)
        {
            base.VisitInputFieldDefinition(value);
            value.SetDirective(this);
        }
    }
}
