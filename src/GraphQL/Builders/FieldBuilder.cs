using System;
using GraphQL.Types;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TGraphType, object, TGraphType> Create<TGraphType>()
            where TGraphType : GraphType
        {
            return FieldBuilder<TGraphType, object, TGraphType>.Create();
        }
    }

    public class FieldBuilder<TGraphType, TObjectType, TReturnType>
        where TGraphType : GraphType
    {
        public class ResolutionArguments
        {
            private readonly ResolveFieldContext _context;

            internal ResolutionArguments(ResolveFieldContext context, Func<object, TObjectType> objectResolver = null)
            {
                _context = context;

                if (objectResolver != null)
                {
                    Object = objectResolver(context.Source);
                }
                else if (context.Source is TObjectType)
                {
                    Object = (TObjectType)context.Source;
                }
            }

            public ISchema Schema { get { return _context.Schema; } }

            public object Source { get { return _context.Source; } }

            public TObjectType Object { get; set; }

            public T GetArgument<T>(string argumentName, T defaultValue = default(T))
            {
                object argument;

                if (_context.Arguments.TryGetValue(argumentName, out argument))
                {
                    return argument is T ? (T)argument : defaultValue;
                }

                return defaultValue;
            }

            public bool HasArgument(string argumentName)
            {
                return _context.Arguments.ContainsKey(argumentName);
            }
        }

        private readonly Func<object, TObjectType> _objectResolver;

        public FieldType FieldType { get; protected set; }

        private FieldBuilder(FieldType fieldType, Func<object, TObjectType> objectResolver)
        {
            _objectResolver = objectResolver;
            FieldType = fieldType;
        }

        public static FieldBuilder<TGraphType, TObjectType, TReturnType> Create()
        {
            var fieldType = new FieldType
            {
                Type = typeof(TGraphType),
                Arguments = new QueryArguments(new QueryArgument[0]),
            };
            return new FieldBuilder<TGraphType, TObjectType, TReturnType>(fieldType, null);
        }

        public FieldBuilder<TGraphType, TObjectType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public FieldBuilder<TGraphType, TObjectType, TReturnType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public FieldBuilder<TGraphType, TObjectType, TReturnType> DefaultValue(TReturnType defaultValue = default(TReturnType))
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        public FieldBuilder<TGraphType, TNewObjectType, TReturnType> WithObject<TNewObjectType>(Func<object, TNewObjectType> objectResolver = null)
        {
            return new FieldBuilder<TGraphType, TNewObjectType, TReturnType>(FieldType, objectResolver ?? (obj => (TNewObjectType)obj));
        }

        public FieldBuilder<TGraphType, TObjectType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new FieldBuilder<TGraphType, TObjectType, TNewReturnType>(FieldType, _objectResolver);
        }

        public FieldBuilder<TGraphType, TObjectType, TReturnType> Argument<TArgumentGraphType>(string name, string description)
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public FieldBuilder<TGraphType, TObjectType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        public void Resolve(Func<ResolutionArguments, TReturnType> resolver)
        {
            FieldType.Resolve = context => resolver(new ResolutionArguments(context, _objectResolver));
        }
    }
}
