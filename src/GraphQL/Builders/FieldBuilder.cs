using System;
using GraphQL.Types;
using GraphQL.Resolvers;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(Type type = null)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }
    }

    public class FieldBuilder<TSourceType, TReturnType>
    {
        private readonly FieldType _fieldType;

        public FieldType FieldType => _fieldType;

        private FieldBuilder(FieldType fieldType)
        {
            _fieldType = fieldType;
        }

        public static FieldBuilder<TSourceType, TReturnType> Create(Type type = null)
        {
            var fieldType = new FieldType
            {
                Type = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        public FieldBuilder<TSourceType, TReturnType> Type(IGraphType type)
        {
            _fieldType.ResolvedType = type;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Name(string name)
        {
            _fieldType.Name = name;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Description(string description)
        {
            _fieldType.Description = description;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> DeprecationReason(string deprecationReason)
        {
            _fieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType defaultValue = default(TReturnType))
        {
            _fieldType.DefaultValue = defaultValue;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver resolver)
        {
            _fieldType.Resolver = resolver;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<ResolveFieldContext<TSourceType>, TReturnType> resolve)
        {
            return Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));
        }

        public FieldBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new FieldBuilder<TSourceType, TNewReturnType>(FieldType);
        }

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string description)
        {
            _fieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
        {
            _fieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }
    }
}
