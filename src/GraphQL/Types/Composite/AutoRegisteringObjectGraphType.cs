using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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
            ConfigureGraph();
            AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
        }

        /// <summary>
        /// Applies default configuration settings to this graph type prior to applying <see cref="GraphQLAttribute"/> attributes.
        /// Allows the ability to override the default naming convention used by this class without affecting attributes applied directly to this class.
        /// </summary>
        protected virtual void ConfigureGraph() { }

        /// <summary>
        /// Returns a list of <see cref="FieldType"/> instances representing the fields ready to be
        /// added to the graph type.
        /// </summary>
        protected virtual IEnumerable<FieldType> ProvideFields()
        {
            foreach (var memberInfo in GetRegisteredMembers())
            {
                if (memberInfo.IsDefined(typeof(IgnoreAttribute)))
                    continue;
                var fieldType = CreateField(memberInfo);
                if (fieldType != null)
                    yield return fieldType;
            }
        }

        /// <summary>
        /// Processes the specified member and returns a <see cref="FieldType"/>.
        /// May return <see langword="null"/> to skip a member.
        /// </summary>
        protected virtual FieldType? CreateField(MemberInfo memberInfo)
        {
            var typeInformation = GetTypeInformation(memberInfo);
            var graphType = typeInformation.ConstructGraphType();
            return AutoRegisteringHelper.CreateField(memberInfo, graphType, false);
        }

        /// <summary>
        /// Returns a list of properties, methods or fields that should have fields created for them.
        /// <br/><br/>
        /// Unless overridden, returns a list of public instance readable properties (including properties declared on
        /// inherited classes) and public instance methods (excluding methods declared on inherited classes) that
        /// do not return <see langword="void"/> or <see cref="Task"/>.
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
        {
            var props = AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead),
                _excludedProperties);
            var methods = typeof(TSourceType).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x =>
                    !x.ContainsGenericParameters && //exclude methods with open generics
                    !x.IsSpecialName &&             //exclude methods generated for properties
                    x.ReturnType != typeof(void) && //exclude methods which do not return a value
                    x.ReturnType != typeof(Task) && //exclude methods which do not return a value
                    x.GetParameters().Length == 0); //exclude methods which contain arguments
            return props.Concat<MemberInfo>(methods);
        }

        /// <summary>
        /// Analyzes a property, method or field and returns an instance of <see cref="TypeInformation"/>
        /// containing information necessary to select a graph type. Nullable reference annotations
        /// are read, if they exist, as well as the <see cref="RequiredAttribute"/> attribute.
        /// Then any <see cref="GraphQLAttribute"/> attributes marked on the property are applied.
        /// <br/><br/>
        /// Override this method to enforce specific graph types for specific CLR types, or to implement custom
        /// attributes to change graph type selection behavior.
        /// </summary>
        protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
        {
            var typeInformation = memberInfo switch
            {
                PropertyInfo propertyInfo => new TypeInformation(propertyInfo, false),
                MethodInfo methodInfo => new TypeInformation(methodInfo),
                FieldInfo fieldInfo => new TypeInformation(fieldInfo, false),
                _ => throw new ArgumentOutOfRangeException(nameof(memberInfo), "Only properties, methods and fields are supported."),
            };
            typeInformation.ApplyAttributes();
            return typeInformation;
        }
    }
}
