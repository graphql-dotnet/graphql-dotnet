using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Types
{
    /// <summary>
    /// Helper methods for auto-registering graph types, <see cref="Builders.FieldBuilder{TSourceType, TReturnType}.ResolveDelegate(Delegate?)">Resolve</see>,
    /// schema builder method builders, and <see cref="NameFieldResolver"/>.
    /// </summary>
    public static class AutoRegisteringHelper
    {
        /// <summary>
        /// Constructs a field resolver for the specified field, property or method with the specified instance expression.
        /// Does not build accompanying query arguments for detected method parameters.
        /// Does not allow overriding build behavior.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
        public static IFieldResolver BuildFieldResolver(MemberInfo memberInfo, Type? sourceType, FieldType? fieldType, LambdaExpression instanceExpression)
        {
            // this entire method is a simplification of AutoRegisteringObjectGraphType.BuildFieldType
            // but it does not provide the ability to override any behavior, and it does not return or
            // build query arguments
            if (memberInfo is FieldInfo fieldInfo)
            {
                return new MemberResolver(fieldInfo, instanceExpression);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                return new MemberResolver(propertyInfo, instanceExpression);
            }
            else if (memberInfo is MethodInfo methodInfo)
            {
                var arguments = BuildFieldResolver_BuildMethodArguments(methodInfo, sourceType, fieldType);
                return new MemberResolver(methodInfo, instanceExpression, arguments);
            }

            throw new ArgumentOutOfRangeException(nameof(memberInfo), "Member must be a field, property or method.");
        }

        /// <summary>
        /// Constructs an event stream resolver for the specified method with the specified instance expression.
        /// Does not build accompanying query arguments for detected method parameters.
        /// Does not allow overriding build behavior.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
        public static ISourceStreamResolver BuildSourceStreamResolver(MethodInfo methodInfo, Type? sourceType, FieldType? fieldType, LambdaExpression instanceExpression)
        {
            var arguments = BuildFieldResolver_BuildMethodArguments(methodInfo, sourceType, fieldType);
            return new SourceStreamMethodResolver(methodInfo, instanceExpression, arguments);
        }

        private static IList<LambdaExpression> BuildFieldResolver_BuildMethodArguments(MethodInfo methodInfo, Type? sourceType, FieldType? fieldType)
        {
            List<LambdaExpression> expressions = new();
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var typeInformation = new TypeInformation(parameterInfo);
                typeInformation.ApplyAttributes(); // typically this is unnecessary, since this is primarily used to control the graph type of generated query arguments
                var argumentInfo = new ArgumentInformation(parameterInfo, sourceType, fieldType, typeInformation);
                argumentInfo.ApplyAttributes(); // necessary to allow [FromSource], [FromServices] and similar attributes to work
                var (queryArgument, expression) = argumentInfo.ConstructQueryArgument();
                if (queryArgument != null)
                {
                    // even though the query argument is not used, it is necessary to apply attributes to the generated argument in case the name is overridden,
                    // as the generated query argument's name is used within the expression for the call to GetArgument
                    var attributes = parameterInfo.GetGraphQLAttributes();
                    foreach (var attr in attributes)
                        attr.Modify(queryArgument);
                }
                expression ??= GetParameterExpression(
                    parameterInfo.ParameterType,
                    queryArgument ?? throw new InvalidOperationException("Invalid response from ConstructQueryArgument: queryArgument and expression cannot both be null"));
                expressions.Add(expression);
            }
            return expressions;
        }

        /// <summary>
        /// Builds the following instance expression:
        /// <code>context =&gt; context.Source as TSourceType ?? (context.RequestServices ?? serviceProvider).GetService(sourceType) ?? throw new InvalidOperationException(...)</code>
        /// </summary>
        internal static LambdaExpression BuildInstanceExpressionForSchemaBuilder(Type sourceType, IServiceProvider serviceProvider)
        {
            // exception cannot occur here, so don't worry catching TargetInvokeException
            return (LambdaExpression)_buildSourceExpressionForSchemaBuilderInternalMethodInfo
                .MakeGenericMethod(sourceType)
                .Invoke(null, new object[] { serviceProvider })!;
        }

        private static readonly MethodInfo _buildSourceExpressionForSchemaBuilderInternalMethodInfo = typeof(AutoRegisteringHelper).GetMethod(nameof(BuildSourceExpressionForSchemaBuilderInternal), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static Expression<Func<IResolveFieldContext, T>> BuildSourceExpressionForSchemaBuilderInternal<T>(IServiceProvider serviceProvider)
            => context => BuildSourceExpressionForSchemaBuilderInternal_GetSource<T>(context, serviceProvider);
        private static T BuildSourceExpressionForSchemaBuilderInternal_GetSource<T>(IResolveFieldContext context, IServiceProvider serviceProvider)
        {
            var source = context.Source;

            var target = typeof(T).IsInstanceOfType(source)
                ? (T)source!
                : (T?)(context.RequestServices ?? serviceProvider).GetService(typeof(T));

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {typeof(T).Name} to execute {parentType}{context.FieldAst.Name}");
            }

            return target;
        }

        /// <summary>
        /// Scans a specific CLR type for <see cref="GraphQLAttribute"/> attributes and applies
        /// them to the specified <see cref="IGraphType"/>.
        /// Also scans the CLR type's owning module and assembly for globally-applied attributes.
        /// </summary>
        internal static void ApplyGraphQLAttributes<TSourceType>(IGraphType graphType)
        {
            // Description and deprecation reason are already set in ComplexGraphType<TSourceType> constructor

            // Apply derivatives of GraphQLAttribute
            var attributes = typeof(TSourceType).GetGraphQLAttributes();
            foreach (var attr in attributes)
            {
                attr.Modify(graphType);
            }
        }

        /// <summary>
        /// Filters an enumeration of <see cref="PropertyInfo"/> values to exclude specified properties.
        /// </summary>
        internal static IEnumerable<PropertyInfo> ExcludeProperties<TSourceType>(IEnumerable<PropertyInfo> properties, params Expression<Func<TSourceType, object?>>[]? excludedProperties)
            => excludedProperties == null || excludedProperties.Length == 0
                ? properties
                : properties.Where(propertyInfo => !excludedProperties!.Any(p => GetPropertyName(p) == propertyInfo.Name));

        /// <summary>
        /// Creates a <see cref="FieldType"/> for the specified <see cref="MemberInfo"/>.
        /// </summary>
        internal static FieldType CreateField(MemberInfo memberInfo, Type graphType, bool isInputType)
        {
            var fieldType = new FieldType()
            {
                Name = memberInfo.Name,
                Description = memberInfo.Description(),
                DeprecationReason = memberInfo.ObsoleteMessage(),
                Type = graphType,
                DefaultValue = isInputType ? memberInfo.DefaultValue() : null,
            };
            if (isInputType)
            {
                fieldType.WithMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, memberInfo.Name);
            }
            if (!isInputType &&
                memberInfo is MethodInfo methodInfo &&
                fieldType.Name.EndsWith("Async") &&
                methodInfo.ReturnType.IsGenericType &&
                methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                fieldType.Name = fieldType.Name.Substring(0, fieldType.Name.Length - 5);
            }

            return fieldType;
        }

        /// <summary>
        /// Applies <see cref="GraphQLAttribute"/>s defined on <paramref name="memberInfo"/> to <paramref name="fieldType"/>.
        /// Also scans the member's owning module and assembly for globally-applied attributes.
        /// </summary>
        internal static void ApplyFieldAttributes(MemberInfo memberInfo, FieldType fieldType, bool isInputType)
        {
            // Apply derivatives of GraphQLAttribute
            var attributes = memberInfo.GetGraphQLAttributes();
            foreach (var attr in attributes)
            {
                attr.Modify(fieldType, isInputType);
            }
        }

        private static string GetPropertyName<TSourceType>(Expression<Func<TSourceType, object?>> expression)
        {
            if (expression.Body is MemberExpression m1)
                return m1.Member.Name;

            if (expression.Body is UnaryExpression u && u.Operand is MemberExpression m2)
                return m2.Member.Name;

            throw new NotSupportedException($"Unsupported type of expression: {expression.GetType().Name}");
        }

        /// <summary>
        /// Constructs a lambda expression for a field resolver to return the specified query argument
        /// from the resolve context. The returned lambda is similar to the following:
        /// <code>context =&gt; context.GetArgument&lt;T&gt;(queryArgument.Name, queryArgument.DefaultValue)</code>
        /// </summary>
        internal static LambdaExpression GetParameterExpression(Type parameterType, QueryArgument queryArgument)
        {
            //construct a typed call to AutoRegisteringHelper.GetArgumentInternal, passing in queryArgument
            var getArgumentMethodTyped = _getArgumentMethod.MakeGenericMethod(parameterType);
            var resolveFieldContextParameter = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var queryArgumentExpression = Expression.Constant(queryArgument, typeof(QueryArgument));
            //e.g. Func<IResolveFieldContext, int> = (context) => AutoRegisteringHelper.GetArgumentInternal<int>(context, queryArgument);
            var expr = Expression.Call(getArgumentMethodTyped, resolveFieldContextParameter, queryArgumentExpression);
            return Expression.Lambda(expr, resolveFieldContextParameter);
        }

        private static readonly MethodInfo _getArgumentMethod = typeof(AutoRegisteringHelper).GetMethod(nameof(GetArgumentInternal), BindingFlags.NonPublic | BindingFlags.Static)!;
        /// <summary>
        /// Returns the value for the specified query argument, or the default value of the query argument
        /// if a value was not specified in the request.
        /// <br/><br/>
        /// Unlike <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>,
        /// the default value is not returned if <see langword="null"/> was explicitly supplied within the query.
        /// The default value is only returned if no value was supplied to the query.
        /// </summary>
        private static T? GetArgumentInternal<T>(IResolveFieldContext context, QueryArgument queryArgument)
        {
            // GetArgument changes null values to DefaultValue even if null is explicitly specified,
            // so do not use GetArgument here
            return context.TryGetArgument(typeof(T), queryArgument.Name, out var value)
                ? (T?)value
                : (T?)queryArgument.DefaultValue;
        }

        /// <summary>
        /// Configures query arguments and a field resolver for the specified <see cref="FieldType"/>, overwriting
        /// any existing configuration within <see cref="FieldType.Arguments"/>, <see cref="FieldType.Resolver"/>
        /// and <see cref="FieldType.StreamResolver"/>.
        /// <br/><br/>
        /// For fields and properties, no query arguments are added and the field resolver simply pulls the appropriate
        /// member from <see cref="IResolveFieldContext.Source"/>.
        /// <br/><br/>
        /// For methods, method arguments are iterated and processed by
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}.GetArgumentInformation{TParameterType}(FieldType, ParameterInfo)">GetArgumentInformation</see>, building
        /// a list of query arguments and expressions as necessary. Then a field resolver is built around the method.
        /// </summary>
        internal static void BuildFieldTypeForOutput(
            MemberInfo memberInfo,
            FieldType fieldType,
            Func<MemberInfo, LambdaExpression> BuildMemberInstanceExpression,
            Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod,
            Action<ParameterInfo, QueryArgument> ApplyArgumentAttributes)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                var resolver = new MemberResolver(propertyInfo, BuildMemberInstanceExpression(memberInfo));
                fieldType.Arguments = null;
                fieldType.Resolver = resolver;
                fieldType.StreamResolver = null;
            }
            else if (memberInfo is MethodInfo methodInfo)
            {
                List<LambdaExpression> expressions = new();
                QueryArguments queryArguments = new();
                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    var getArgumentInfoMethod = getTypedArgumentInfoMethod(parameterInfo.ParameterType);
                    var argumentInfo = getArgumentInfoMethod(fieldType, parameterInfo);
                    var (queryArgument, expression) = argumentInfo.ConstructQueryArgument();
                    if (queryArgument != null)
                    {
                        ApplyArgumentAttributes(parameterInfo, queryArgument);
                        queryArguments.Add(queryArgument);
                    }
                    expression ??= AutoRegisteringHelper.GetParameterExpression(
                        parameterInfo.ParameterType,
                        queryArgument ?? throw new InvalidOperationException("Invalid response from ConstructQueryArgument: queryArgument and expression cannot both be null"));
                    expressions.Add(expression);
                }
                var memberInstanceExpression = BuildMemberInstanceExpression(methodInfo);
                if (IsObservable(methodInfo.ReturnType))
                {
                    var resolver = new SourceStreamMethodResolver(methodInfo, memberInstanceExpression, expressions);
                    fieldType.Resolver = resolver;
                    fieldType.StreamResolver = resolver;
                }
                else
                {
                    var resolver = new MemberResolver(methodInfo, memberInstanceExpression, expressions);
                    fieldType.Resolver = resolver;
                    fieldType.StreamResolver = null;
                }
                fieldType.Arguments = queryArguments;
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                var resolver = new MemberResolver(fieldInfo, BuildMemberInstanceExpression(memberInfo));
                fieldType.Arguments = null;
                fieldType.Resolver = resolver;
                fieldType.StreamResolver = null;
            }
            else if (memberInfo == null)
            {
                throw new ArgumentNullException(nameof(memberInfo));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(memberInfo), "Member must be a field, property or method.");
            }
        }

        /// <summary>
        /// Determines if the type is an <see cref="IObservable{T}"/> or task that returns an <see cref="IObservable{T}"/>.
        /// </summary>
        private static bool IsObservable(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var g = type.GetGenericTypeDefinition();
            if (g == typeof(IObservable<>))
                return true;
            if (g == typeof(Task<>) || g == typeof(ValueTask<>))
                return IsObservable(type.GetGenericArguments()[0]);
            return false;
        }

        /// <summary>
        /// Returns a list of properties, methods or fields that should have fields created for them.
        /// <br/><br/>
        /// Unless overridden, returns a list of public instance readable properties and public instance methods
        /// that do not return <see langword="void"/> or <see cref="Task"/>
        /// including properties and methods declared on inherited classes.
        /// </summary>
        internal static IEnumerable<MemberInfo> GetRegisteredMembersForOutput<TSourceType>(Expression<Func<TSourceType, object?>>[]? excludedProperties)
        {
            if (typeof(TSourceType).IsInterface)
            {
                var types = typeof(TSourceType).GetInterfaces().Append(typeof(TSourceType));

                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                var properties = ExcludeProperties(
                    types.SelectMany(type => type.GetProperties(flags)).Where(x => x.CanRead),
                    excludedProperties);
                var methods = types.SelectMany(type => type.GetMethods(flags))
                    .Where(x =>
                        !x.ContainsGenericParameters &&     // exclude methods with open generics
                        !x.IsSpecialName &&                 // exclude methods generated for properties
                        x.ReturnType != typeof(void) &&     // exclude methods which do not return a value
                        x.ReturnType != typeof(Task));
                return properties.Concat<MemberInfo>(methods);
            }
            else
            {
                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                var properties = ExcludeProperties(
                    typeof(TSourceType).GetProperties(flags).Where(x => x.CanRead),
                    excludedProperties);
                var methods = typeof(TSourceType).GetMethods(flags)
                    .Where(x =>
                        !x.ContainsGenericParameters &&                          // exclude methods with open generics
                        !x.IsSpecialName &&                                      // exclude methods generated for properties
                        x.ReturnType != typeof(void) &&                          // exclude methods which do not return a value
                        x.ReturnType != typeof(Task) &&                          // exclude methods which do not return a value
                        x.GetBaseDefinition().DeclaringType != typeof(object) && // exclude methods inherited from object (e.g. GetHashCode)
                                                                                 // exclude methods generated for record types: bool Equals(TSourceType)
                        !(x.Name == "Equals" && !x.IsStatic && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(TSourceType) && x.ReturnType == typeof(bool)) &&
                        x.Name != "<Clone>$");                                   // exclude methods generated for record types
                return properties.Concat<MemberInfo>(methods);
            }
        }
    }
}
