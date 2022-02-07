using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    /// <summary>
    /// Contains information pertaining to a method parameter in preparation for buliding an
    /// expression or query argument for it.
    /// <br/><br/>
    /// If <see cref="ArgumentInformation.Expression"/> is set, a query argument will not be added
    /// and the expression will be used to build the method resolver.
    /// <br/><br/>
    /// If not, a query argument will be generated and added to the field; the field resolver will
    /// use the argument's value to populate the method parameter.
    /// </summary>
    public abstract class ArgumentInformation
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        protected ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation, LambdaExpression? expression)
        {
            ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            FieldType = fieldType ?? throw new ArgumentNullException(nameof(fieldType));
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            TypeInformation = typeInformation ?? throw new ArgumentNullException(nameof(typeInformation));
            Expression = expression;
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// If the parameter type is <see cref="IResolveFieldContext"/> or <see cref="CancellationToken"/>,
        /// an expression is generated for the parameter and set within <see cref="Expression"/>; otherwise
        /// <see cref="Expression"/> is set to <see langword="null"/>.
        /// </summary>
        protected ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation)
            : this(parameterInfo, sourceType, fieldType, typeInformation, null)
        {
            if (parameterInfo.ParameterType == typeof(IResolveFieldContext))
            {
                Expression<Func<IResolveFieldContext, IResolveFieldContext>> expr = x => x;
                Expression = expr;
            }
            else if (parameterInfo.ParameterType == typeof(CancellationToken))
            {
                Expression<Func<IResolveFieldContext, CancellationToken>> expr = x => x.CancellationToken;
                Expression = expr;
            }
        }

        /// <summary>
        /// The method parameter.
        /// </summary>
        public ParameterInfo ParameterInfo { get; }

        /// <summary>
        /// The expected type of <see cref="IResolveFieldContext.Source"/>.
        /// Should equal <c>TSourceType</c> within <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// The <see cref="Types.FieldType"/> that the query argument will be added to.
        /// </summary>
        public FieldType FieldType { get; }

        /// <summary>
        /// The parsed type information of the method parameter.
        /// </summary>
        public TypeInformation TypeInformation { get; }

        private LambdaExpression? _expression;
        /// <summary>
        /// Gets or sets a delegate in the form of a <see cref="LambdaExpression"/> to be used to populate
        /// this method argument while building the field resolver.
        /// <br/><br/>
        /// If not set, a query argument will be added to the field and the argument's value will be used
        /// to populate the method argument while building the field resolver.
        /// <br/><br/>
        /// The delegate must be of the type
        /// <see cref="Expression{TDelegate}">Expression</see>&lt;<see cref="Func{T, TResult}">Func</see>&lt;<see cref="IResolveFieldContext"/>, TParameterType&gt;&gt;
        /// where TParameterType matches <see cref="ParameterInfo">ParameterInfo</see>.<see cref="ParameterInfo.ParameterType">ParameterType</see>.
        /// </summary>
        public LambdaExpression? Expression
        {
            get => _expression;
            set
            {
                if (value != null && (value.ReturnType != ParameterInfo.ParameterType || value.Parameters.Count != 1 || value.Parameters[0].Type != typeof(IResolveFieldContext)))
                {
                    throw new ArgumentException($"Value must be a lambda expression delegate of type Func<IResolveFieldContext, {ParameterInfo.ParameterType.Name}>.");
                }
                _expression = value;
            }
        }

        /// <summary>
        /// Builds a query argument or expression from this instance.
        /// <br/><br/>
        /// If a query argument is returned, it will be added to the arguments list of the field type.
        /// <br/><br/>
        /// If an expression is returned, it will be used to populate the method argument within the field resolver;
        /// if not, the query argument's value will be used to populate the method argument within the field resolver.
        /// <br/><br/>
        /// The default implementation will return either a <see cref="QueryArgument"/> or <see cref="LambdaExpression"/>
        /// instance; not both. It is possible to return both, in which case the query argument will be added to the
        /// field and the expression will be used to populate the method argument within the field resolver.
        /// You cannot return <see langword="null"/> for both the query argument and expression.
        /// </summary>
        public virtual (QueryArgument? QueryArgument, LambdaExpression? Expression) ConstructQueryArgument()
        {
            if (Expression != null)
                return (null, Expression);

            var type = TypeInformation.ConstructGraphType();
            var argument = new QueryArgument(type)
            {
                Name = ParameterInfo.Name!,
                Description = ParameterInfo.Description(),
                DefaultValue = ParameterInfo.IsOptional ? ParameterInfo.DefaultValue : null,
            };
            return (argument, null);
        }
    }

    /// <inheritdoc/>
    public class ArgumentInformation<TParameterType> : ArgumentInformation
    {
        /// <inheritdoc cref="ArgumentInformation.ArgumentInformation(ParameterInfo, Type, FieldType, TypeInformation, LambdaExpression?)"/>
        public ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation, Expression<Func<IResolveFieldContext, TParameterType>>? expression)
            : base(ValidateParameterInfo(parameterInfo), sourceType, fieldType, typeInformation, expression)
        {
        }

        /// <inheritdoc/>
        public ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation)
            : base(ValidateParameterInfo(parameterInfo), sourceType, fieldType, typeInformation)
        {
        }

        /// <summary>
        /// Validates that the <see cref="ParameterInfo"/> supplied to the constructor has a parameter type
        /// that matches the <typeparamref name="TParameterType"/> of this instance.
        /// </summary>
        private static ParameterInfo ValidateParameterInfo(ParameterInfo parameterInfo)
        {
            if (parameterInfo.ParameterType != typeof(TParameterType))
            {
                throw new ArgumentOutOfRangeException(nameof(parameterInfo), $"Parameter must have a return type of {typeof(TParameterType).Name}.");
            }
            return parameterInfo;
        }

        /// <summary>
        /// Gets or sets a delegate in the form of a <see cref="LambdaExpression"/> to be used to populate
        /// this method argument while building the field resolver.
        /// <br/><br/>
        /// If not set, a query argument will be added to the field arguments list and the argument's value will be used
        /// to populate the method argument while building the field resolver.
        /// <br/><br/>
        /// The lambda must be of the type <see cref="Expression{TDelegate}">Expression</see>&lt;<see cref="Func{T, TResult}">Func</see>&lt;<see cref="IResolveFieldContext"/>, <typeparamref name="TParameterType"/>&gt;&gt;
        /// where <typeparamref name="TParameterType"/> is the parameter type.
        /// </summary>
        public new Expression<Func<IResolveFieldContext, TParameterType?>>? Expression
        {
            get => (Expression<Func<IResolveFieldContext, TParameterType?>>?)base.Expression;
            set => base.Expression = value;
        }

        /// <summary>
        /// Applies <see cref="GraphQLAttribute"/> attributes pulled from the <see cref="ArgumentInformation.ParameterInfo">ParameterInfo</see> onto this instance.
        /// </summary>
        public virtual void ApplyAttributes()
        {
            var attributes = ParameterInfo.GetCustomAttributes(typeof(GraphQLAttribute), false);
            foreach (var attr in attributes)
            {
                ((GraphQLAttribute)attr).Modify(this);
            }
        }
    }
}
