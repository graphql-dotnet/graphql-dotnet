using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace GraphQL.Types
{
    public abstract class ArgumentInformation
    {
        protected ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation, LambdaExpression? expression)
        {
            ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            FieldType = fieldType ?? throw new ArgumentNullException(nameof(fieldType));
            SourceType = sourceType ?? throw new ArgumentNullException();
            TypeInformation = typeInformation ?? throw new ArgumentNullException(nameof(typeInformation));
            Expression = expression;
        }

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

        public ParameterInfo ParameterInfo { get; }

        public Type SourceType { get; }

        public FieldType FieldType { get; }

        public TypeInformation TypeInformation { get; }

        private LambdaExpression? _expression;
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

        //public static ArgumentInformation Create(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation)
        //{
        //    var constructedType = typeof(ArgumentInformation<>).MakeGenericType(parameterInfo.ParameterType);
        //    var constructor = constructedType.GetConstructors().First(x => x.GetParameters().Length == 4);
        //    return (ArgumentInformation)constructor.Invoke(new object[] { parameterInfo, sourceType, fieldType, typeInformation });
        //}
    }

    public class ArgumentInformation<TReturnType> : ArgumentInformation
    {
        public ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation, Expression<Func<IResolveFieldContext, TReturnType>>? expression)
            : base(ValidateParameterInfo(parameterInfo), sourceType, fieldType, typeInformation, expression)
        {
        }

        public ArgumentInformation(ParameterInfo parameterInfo, Type sourceType, FieldType fieldType, TypeInformation typeInformation)
            : base(ValidateParameterInfo(parameterInfo), sourceType, fieldType, typeInformation)
        {
        }

        private static ParameterInfo ValidateParameterInfo(ParameterInfo parameterInfo)
        {
            if (parameterInfo.ParameterType != typeof(TReturnType))
            {
                throw new ArgumentOutOfRangeException(nameof(parameterInfo), $"Parameter must have a return type of {typeof(TReturnType).Name}.");
            }
            return parameterInfo;
        }

        public new Expression<Func<IResolveFieldContext, TReturnType>>? Expression
        {
            get => (Expression<Func<IResolveFieldContext, TReturnType>>?)base.Expression;
            set => base.Expression = value;
        }

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
