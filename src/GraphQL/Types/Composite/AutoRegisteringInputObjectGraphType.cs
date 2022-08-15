using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified input type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// Note that now __InputValue has no isDeprecated and deprecationReason fields but in the future they may appear - https://github.com/graphql/graphql-spec/pull/525
    /// </summary>
    public class AutoRegisteringInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
    {
        private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringInputObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringInputObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            _excludedProperties = excludedProperties;
            Name = typeof(TSourceType).GraphQLName();
            ConfigureGraph();
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
        }

        /// <summary>
        /// Applies default configuration settings to this graph type along with any <see cref="GraphQLAttribute"/> attributes marked on <typeparamref name="TSourceType"/>.
        /// Allows the ability to override the default naming convention used by this class without affecting attributes applied directly to <typeparamref name="TSourceType"/>.
        /// </summary>
        protected virtual void ConfigureGraph()
        {
            AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
        }

        /// <inheritdoc cref="AutoRegisteringHelper.ProvideFields(IEnumerable{MemberInfo}, Func{MemberInfo, FieldType?}, bool)"/>
        protected virtual IEnumerable<FieldType> ProvideFields()
            => AutoRegisteringHelper.ProvideFields(GetRegisteredMembers(), CreateField, true);

        /// <summary>
        /// Processes the specified property or field and returns a <see cref="FieldType"/>.
        /// May return <see langword="null"/> to skip a property.
        /// </summary>
        protected virtual FieldType? CreateField(MemberInfo memberInfo)
            => AutoRegisteringHelper.CreateField(memberInfo, GetTypeInformation, null, true);

        /// <summary>
        /// Returns a list of properties or fields that should have fields created for them.
        /// Unless overridden, returns a list of public instance writable properties,
        /// including properties on inherited classes.
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
            => AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite),
                _excludedProperties);

        /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(MemberInfo, bool)"/>
        /// <remarks>
        /// Only properties and fields are supported.
        /// </remarks>
        protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
            => AutoRegisteringHelper.GetTypeInformation(memberInfo, true);
    }
}
