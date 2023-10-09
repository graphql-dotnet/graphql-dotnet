using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    internal static class AutoRegisteringObjectGraphType
    {
        public static ConcurrentDictionary<Type, ObjectGraphType> ReflectionCache { get; } = new();
    }

    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// </summary>
    public class AutoRegisteringObjectGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] TSourceType> : ObjectGraphType<TSourceType>
    {
        private readonly Expression<Func<TSourceType, object?>>[]? _excludedProperties;

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties">Expressions for excluding fields, for example 'o => o.Age'.</param>
        public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            if (GlobalSwitches.EnableReflectionCaching && excludedProperties == null && AutoRegisteringObjectGraphType.ReflectionCache.TryGetValue(GetType(), out var cacheEntry))
            {
                // restore the cached properties and skip all reflection and dynamic compilation otherwise necessary
                RestoreCacheEntry(cacheEntry);
                return;
            }
            _excludedProperties = excludedProperties;
            Name = typeof(TSourceType).GraphQLName();
            ConfigureGraph();
            foreach (var fieldType in ProvideFields())
            {
                _ = AddField(fieldType);
            }
            if (GlobalSwitches.EnableReflectionCaching && excludedProperties == null)
            {
                // cache the constructed object
                var entry = CreateCacheEntry();
                if (entry != null)
                    AutoRegisteringObjectGraphType.ReflectionCache[GetType()] = entry;
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
            => AutoRegisteringHelper.ProvideFields(GetRegisteredMembers(), CreateField, false);

        /// <summary>
        /// Processes the specified member and returns a <see cref="FieldType"/>.
        /// May return <see langword="null"/> to skip a member.
        /// </summary>
        protected virtual FieldType? CreateField(MemberInfo memberInfo)
            => AutoRegisteringHelper.CreateField(memberInfo, GetTypeInformation, BuildFieldType, false);

        /// <inheritdoc cref="AutoRegisteringOutputHelper.BuildFieldType(MemberInfo, FieldType, Func{MemberInfo, LambdaExpression}, Func{Type, Func{FieldType, ParameterInfo, ArgumentInformation}}, Action{ParameterInfo, QueryArgument})"/>
        protected void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
        {
            Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod =
                parameterType =>
                {
                    var getArgumentInfoMethodInfo = _getArgumentInformationInternalMethodInfo.MakeGenericMethod(parameterType);
                    return (Func<FieldType, ParameterInfo, ArgumentInformation>)getArgumentInfoMethodInfo.CreateDelegate(typeof(Func<FieldType, ParameterInfo, ArgumentInformation>), this);
                };

            AutoRegisteringOutputHelper.BuildFieldType(
                memberInfo,
                fieldType,
                BuildMemberInstanceExpression,
                getTypedArgumentInfoMethod,
                ApplyArgumentAttributes);
        }

        /// <summary>
        /// Returns a lambda expression that will be used by the field resolver to access the member.
        /// <br/><br/>
        /// Typically this is a lambda expression of type <see cref="Func{T, TResult}">Func</see>&lt;<see cref="IResolveFieldContext"/>, <typeparamref name="TSourceType"/>&gt;.
        /// <br/><br/>
        /// By default this returns the <see cref="IResolveFieldContext.Source"/> property.
        /// </summary>
        /// <param name="memberInfo">The member being called or accessed.</param>
        protected virtual LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo)
            => _sourceExpression;

        private static readonly Expression<Func<IResolveFieldContext, TSourceType>> _sourceExpression
            = context => (TSourceType)(context.Source ?? ThrowSourceNullException());

        private static object ThrowSourceNullException()
        {
            throw new InvalidOperationException("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        }

        private static readonly MethodInfo _getArgumentInformationInternalMethodInfo = typeof(AutoRegisteringObjectGraphType<TSourceType>).GetMethod(nameof(GetArgumentInformationInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private ArgumentInformation GetArgumentInformationInternal<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
            => GetArgumentInformation<TParameterType>(fieldType, parameterInfo);

        /// <inheritdoc cref="AutoRegisteringOutputHelper.ApplyArgumentAttributes(ParameterInfo, QueryArgument)"/>
        protected virtual void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
            => AutoRegisteringOutputHelper.ApplyArgumentAttributes(parameterInfo, queryArgument);

        /// <inheritdoc cref="AutoRegisteringOutputHelper.GetArgumentInformation{TSourceType}(TypeInformation, FieldType, ParameterInfo)"/>
        protected virtual ArgumentInformation GetArgumentInformation<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
            => AutoRegisteringOutputHelper.GetArgumentInformation<TSourceType>(GetTypeInformation(parameterInfo), fieldType, parameterInfo);

        /// <inheritdoc cref="AutoRegisteringOutputHelper.GetRegisteredMembers{TSourceType}(Expression{Func{TSourceType, object?}}[])"/>
        protected virtual IEnumerable<MemberInfo> GetRegisteredMembers()
            => AutoRegisteringOutputHelper.GetRegisteredMembers(_excludedProperties);

        /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(MemberInfo, bool)"/>
        protected virtual TypeInformation GetTypeInformation(MemberInfo memberInfo)
            => AutoRegisteringHelper.GetTypeInformation(memberInfo, false);

        /// <inheritdoc cref="AutoRegisteringHelper.GetTypeInformation(ParameterInfo)"/>
        protected virtual TypeInformation GetTypeInformation(ParameterInfo parameterInfo)
            => AutoRegisteringHelper.GetTypeInformation(parameterInfo);

        /// <summary>
        /// Creates a cache entry by deep-copying the current instance's properties.
        /// </summary>
        /// <remarks>
        /// This method performs a deep copy of the current object, including interfaces, fields, and metadata.
        /// However, it skips any resolved types, as they are incompatible with caching.
        /// </remarks>
        /// <returns>
        /// An <see cref="ObjectGraphType"/> representing the cache entry, or null if the instance has any properties 
        /// set that are incompatible with caching (e.g., resolved types).
        /// </returns>
        private ObjectGraphType? CreateCacheEntry()
        {
            // check for any ResolvedType properties set, which would be incompatible with caching
            if (ResolvedInterfaces.Count > 0)
                return null;

            foreach (var f in Fields.List)
            {
                if (f.ResolvedType != null)
                    return null;

                if (f.Arguments?.List != null)
                {
                    foreach (var a in f.Arguments.List)
                    {
                        if (a.ResolvedType != null || a.Type == null)
                            return null;
                    }
                }
            }

            // create cache entry and copy basic props
            var entry = new ObjectGraphType
            {
                Name = Name,
                Description = Description,
                DeprecationReason = DeprecationReason,
                IsTypeOf = IsTypeOf,
            };
            // copy metadata
            CopyMetadataTo(entry);

            // copy interfaces (better to reference the entire Interfaces class, but for now it is only get)
            foreach (var i in Interfaces.List)
            {
                entry.Interfaces.Add(i);
            }

            // copy fields
            foreach (var f in Fields.List)
            {
                var field = new FieldType()
                {
                    Name = f.Name,
                    DeprecationReason = f.DeprecationReason,
                    DefaultValue = f.DefaultValue,
                    Description = f.Description,
                    Resolver = f.Resolver,
                    StreamResolver = f.StreamResolver,
                    Type = f.Type,
                };
                f.CopyMetadataTo(field);
                if (f.Arguments?.List != null && f.Arguments.List.Count > 0)
                {
                    var args = new QueryArguments();
                    foreach (var a in f.Arguments.List)
                    {
                        var arg = new QueryArgument(a.Type!)
                        {
                            Name = a.Name,
                            Description = a.Description,
                            DefaultValue = a.DefaultValue,
                            DeprecationReason = a.DeprecationReason,
                        };
                        a.CopyMetadataTo(arg);
                        args.Add(arg);
                    }
                    field.Arguments = args;
                }
                entry.Fields.Add(field);
            }

            return entry;
        }

        /// <summary>
        /// Restores properties from a cache entry into the current instance.
        /// </summary>
        /// <param name="cacheEntry">The cache entry from which to restore properties.</param>
        private void RestoreCacheEntry(ObjectGraphType cacheEntry)
        {
            // Restore basic props
            Name = cacheEntry.Name;
            Description = cacheEntry.Description;
            DeprecationReason = cacheEntry.DeprecationReason;
            IsTypeOf = cacheEntry.IsTypeOf;

            // Restore metadata
            cacheEntry.CopyMetadataTo(this);

            // Restore interfaces
            foreach (var i in cacheEntry.Interfaces.List)
            {
                Interfaces.Add(i);
            }

            // Restore fields
            foreach (var f in cacheEntry.Fields.List)
            {
                var field = new FieldType()
                {
                    Name = f.Name,
                    DeprecationReason = f.DeprecationReason,
                    DefaultValue = f.DefaultValue,
                    Description = f.Description,
                    Resolver = f.Resolver,
                    StreamResolver = f.StreamResolver,
                    Type = f.Type,
                };
                f.CopyMetadataTo(field);

                if (f.Arguments?.List != null && f.Arguments.List.Count > 0)
                {
                    var args = new QueryArguments();
                    foreach (var a in f.Arguments.List)
                    {
                        var arg = new QueryArgument(a.Type!)
                        {
                            Name = a.Name,
                            Description = a.Description,
                            DefaultValue = a.DefaultValue,
                            DeprecationReason = a.DeprecationReason,
                        };
                        a.CopyMetadataTo(arg);
                        args.Add(arg);
                    }
                    field.Arguments = args;
                }

                Fields.Add(field);
            }
        }
    }
}
