using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// </summary>
    public class AutoRegisteringInterfaceGraphType<TSourceType> : InterfaceGraphType<TSourceType>
    {
        private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringInterfaceGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringInterfaceGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            _excludedProperties = excludedProperties;
            Name = typeof(TSourceType).GraphQLName();
            ConfigureGraph();
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ConfigureGraph"/>
        protected virtual void ConfigureGraph()
        {
            AutoRegisteringHelper.ApplyGraphQLAttributes<TSourceType>(this);
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ProvideFields"/>
        protected virtual IEnumerable<FieldType> ProvideFields()
        {
            foreach (var memberInfo in GetRegisteredMembers())
            {
                bool include = true;
                foreach (var attr in memberInfo.GetGraphQLAttributes())
                {
                    include = attr.ShouldInclude(memberInfo, false);
                    if (!include)
                        break;
                }
                if (!include)
                    continue;
                var fieldType = CreateField(memberInfo);
                if (fieldType != null)
                    yield return fieldType;
            }
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.CreateField(MemberInfo)"/>
        protected virtual FieldType? CreateField(MemberInfo memberInfo)
        {
            var typeInformation = GetTypeInformation(memberInfo);
            var graphType = typeInformation.ConstructGraphType();
            var fieldType = AutoRegisteringHelper.CreateField(memberInfo, graphType, false);
            BuildFieldType(fieldType, memberInfo);
            // apply field attributes after resolver has been set
            AutoRegisteringHelper.ApplyFieldAttributes(memberInfo, fieldType, false);
            return fieldType;
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.BuildFieldType(FieldType, MemberInfo)"/>
        protected void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
        {
            Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod =
                parameterType =>
                {
                    var getArgumentInfoMethodInfo = _getArgumentInformationInternalMethodInfo.MakeGenericMethod(parameterType);
                    return (Func<FieldType, ParameterInfo, ArgumentInformation>)getArgumentInfoMethodInfo.CreateDelegate(typeof(Func<FieldType, ParameterInfo, ArgumentInformation>), this);
                };

            AutoRegisteringHelper.BuildFieldTypeForOutput(
                memberInfo,
                fieldType,
                BuildMemberInstanceExpression,
                getTypedArgumentInfoMethod,
                ApplyArgumentAttributes);
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.BuildMemberInstanceExpression(MemberInfo)"/>
        protected virtual LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
            => _sourceExpression;

        private static readonly Expression<Func<IResolveFieldContext, TSourceType>> _sourceExpression
            = context => (TSourceType)(context.Source ?? ThrowSourceNullException());

        private static object ThrowSourceNullException()
        {
            throw new NullReferenceException("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringInterfaceGraphType as a root graph type or provide a root value.");
        }

        private static readonly MethodInfo _getArgumentInformationInternalMethodInfo = typeof(AutoRegisteringInterfaceGraphType<TSourceType>).GetMethod(nameof(GetArgumentInformationInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private ArgumentInformation GetArgumentInformationInternal<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
            => GetArgumentInformation<TParameterType>(fieldType, parameterInfo);

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.ApplyArgumentAttributes(ParameterInfo, QueryArgument)"/>
        protected virtual void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
        {
            var attributes = parameterInfo.GetGraphQLAttributes();
            foreach (var attr in attributes)
            {
                attr.Modify(queryArgument);
            }
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetArgumentInformation{TParameterType}(FieldType, ParameterInfo)"/>
        protected virtual ArgumentInformation GetArgumentInformation<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
        {
            var typeInformation = GetTypeInformation(parameterInfo);
            var argumentInfo = new ArgumentInformation(parameterInfo, typeof(TSourceType), fieldType, typeInformation);
            argumentInfo.ApplyAttributes();
            return argumentInfo;
        }

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetRegisteredMembers"/>
        protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
            => AutoRegisteringHelper.GetRegisteredMembersForOutput(_excludedProperties);

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(MemberInfo)"/>
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

        /// <inheritdoc cref="AutoRegisteringObjectGraphType{TSourceType}.GetTypeInformation(ParameterInfo)"/>
        protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
        {
            var typeInformation = new TypeInformation(parameterInfo);
            typeInformation.ApplyAttributes();
            return typeInformation;
        }
    }
}
