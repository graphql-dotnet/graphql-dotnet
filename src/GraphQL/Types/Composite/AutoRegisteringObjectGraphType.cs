using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            _excludedProperties = excludedProperties;
            Name = typeof(TSourceType).GraphQLName();
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
        }

        /// <summary>
        /// Returns a list of <see cref="FieldType"/> instances representing the fields ready to be
        /// added to the graph type.
        /// </summary>
        protected virtual IEnumerable<FieldType> ProvideFields()
        {
            foreach (var propertyInfo in GetRegisteredProperties())
            {
                var fieldType = CreateField(propertyInfo);
                if (fieldType != null)
                    yield return fieldType;
            }
        }

        /// <summary>
        /// Processes the specified property and returns a <see cref="FieldType"/>.
        /// May return <see langword="null"/> to skip a property.
        /// </summary>
        protected virtual FieldType? CreateField(PropertyInfo propertyInfo)
            => AutoRegisteringHelper.CreateField(propertyInfo, false);

        /// <summary>
        /// Returns a list of properties that should have fields created for them.
        /// </summary>
        protected virtual IEnumerable<PropertyInfo> GetRegisteredProperties()
            => AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead),
                _excludedProperties);
    }
}
