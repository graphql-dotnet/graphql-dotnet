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
            if (fieldType.Type == typeof(ObjectGraphType))
            {
                throw new ArgumentException("InputObjectGraphType cannot have fields containing a ObjectGraphType.", nameof(fieldType.Type));
            }

            return base.AddField(fieldType);
        }
    }
}

