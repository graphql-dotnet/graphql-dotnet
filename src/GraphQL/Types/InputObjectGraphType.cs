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
            if(fieldType.Type == typeof(ObjectGraphType))
            {
                throw new ArgumentException(nameof(fieldType.Type),
                    "InputObjectGraphType cannot have fields containing a ObjectGraphType.");
            }

            return base.AddField(fieldType);
        }
    }
}

