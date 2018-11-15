using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        public AutoRegisteringObjectGraphType()
        {
            var properties = typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.PropertyType.GetTypeInfo().IsValueType || p.PropertyType == typeof(string));

            foreach (var propertyInfo in properties)
            {
                string t = typeof(TSourceType).Name;
                Field(propertyInfo.PropertyType.GetGraphTypeFromType(propertyInfo.PropertyType.IsNullable()), propertyInfo.Name);
            }
        }

        protected virtual void RemoveField(FieldType fieldType)
        {
            NameValidator.ValidateName(fieldType.Name);

            FieldType field = GetField(fieldType.Name);
            if (field != null)
            {

                
                    ((List<FieldType>)this.Fields).Remove(field);

                
            }
        }

        public void FieldRemove(string name)
        {
            var field = GetField(name);
            if (field != null)
            {
                RemoveField(field);
            }
        }

        public void FieldRemove<TProperty>(Expression<Func<TSourceType, TProperty>> expression)
        {
            string name = expression.NameOf();
            try
            {
                FieldRemove(name);
            }
            catch
            {
                throw new ArgumentException(
                   $"Could not remove the Field {name} inferred from the expression: '{expression.Body}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
        }
    }
}
