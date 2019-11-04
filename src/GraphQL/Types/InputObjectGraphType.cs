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
                throw new ArgumentException("InputObjectGraphType should contain only fields of 'input types' - enumerations, scalars and other InputObjectGraphTypes", nameof(fieldType.Type));
            }

            return base.AddField(fieldType);
        }

        private static bool ValidateFieldType(FieldType fieldType)
        {
            if (fieldType.ResolvedType != null)
            {
                if (fieldType.ResolvedType.IsInputType()) return true;
                if (IsProxyType(fieldType.ResolvedType)) return true;
            }
            if (fieldType.Type != null)
            {
                if (fieldType.Type.IsInputType()) return true;
                if (IsProxyType(fieldType.Type)) return true;
            }
            return false;
        }

        private static bool IsProxyType(IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is GraphQLTypeReference;
        }

        private static bool IsProxyType(Type type)
        {
            var namedType = type.GetNamedType();
            return typeof(GraphQLTypeReference).IsAssignableFrom(namedType);
        }
    }
}

