using System;

namespace GraphQL.Types
{
    public interface IInputObjectGraphType : IComplexGraphType
    {
    }

    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
        public override FieldType AddField(FieldType fieldType)
        {
            if (!ValidateFieldType(fieldType))
            {
                // todo: must fix errors in SchemaBuilder before uncommenting
                // test named "should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field" is disabled until this bug is fixed

                // throw new ArgumentException("InputObjectGraphType cannot have fields containing a ObjectGraphType.", nameof(fieldType.Type));
            }

            return base.AddField(fieldType);
        }

        private static bool ValidateFieldType(FieldType fieldType)
        {
            if (fieldType.ResolvedType != null && fieldType.ResolvedType.IsInputType()) return true;
            if (fieldType.Type != null && fieldType.Type.IsInputType()) return true;
            return false;
        }
    }
}

