using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public static class Arg
    {
        public static ArgumentBuilder Next(string name) => new ArgumentBuilder(name);
    }

    public static class ArgumentBuilderExtensions
    {
        public static ArgumentBuilder Boolean(this ArgumentBuilder builder)
        {
            return builder.Type<BooleanGraphType>();
        }

        public static ArgumentBuilder Int(this ArgumentBuilder builder)
        {
            return builder.Type<IntGraphType>();
        }

        public static ArgumentBuilder String(this ArgumentBuilder builder)
        {
            return builder.Type<StringGraphType>();
        }

        public static ArgumentBuilder DateTime(this ArgumentBuilder builder)
        {
            return builder.Type<DateTimeGraphType>();
        }
    }

    public class ArgumentBuilder
    {
        private readonly string _name;
        private string _descr;
        private object _default;
        private Type _type;
        private IGraphType _resolvedType;
        private bool _nonNull;
        private Dictionary<string, object> _metadata;
        private ArgumentBuilder _next;

        internal ArgumentBuilder(string name)
        {
            _name = name;
        }

        public ArgumentBuilder Description(string description)
        {
            _descr = description;
            return this;
        }

        public ArgumentBuilder Default(object value)
        {
            _default = value;
            return this;
        }

        public ArgumentBuilder Type<TType>()
            where TType: IGraphType
        {
            _type = typeof(TType);
            return this;
        }

        public ArgumentBuilder Type(Type type)
        {
            _type = type;
            return this;
        }

        public ArgumentBuilder ResolvedType(IGraphType type)
        {
            _resolvedType = type;
            return this;
        }

        public ArgumentBuilder NonNull()
        {
            _nonNull = true;
            return this;
        }

        public ArgumentBuilder Metadata(string key, object value)
        {
            (_metadata ??= new Dictionary<string, object>())[key] = value;
            return this;
        }

        public ArgumentBuilder Next(string name)
        {
            _next = new ArgumentBuilder(name);
            return _next;
        }

        public static implicit operator QueryArgument(ArgumentBuilder builder)
        {
            var type = builder._type;
            if (type != null && builder._nonNull)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>))
                {
                    // nothing to do
                }
                else
                {
                    type = typeof(NonNullGraphType<>).MakeGenericType(type);
                }
            }

            QueryArgument arg;

            if (type != null)
            {
                arg = new QueryArgument(type)
                {
                    Name = builder._name,
                    Description = builder._descr,
                    DefaultValue = builder._default,
                };
                if (builder._resolvedType != null)
                    arg.ResolvedType = builder._resolvedType;
            }
            else if (builder._resolvedType != null)
            {
                arg = new QueryArgument(builder._resolvedType)
                {
                    Name = builder._name,
                    Description = builder._descr,
                    DefaultValue = builder._default,
                };
            }
            else
            {
                throw new InvalidOperationException("Either Type or ResolvedType method should be called.");
            }

            if (builder._metadata != null)
                foreach (var item in builder._metadata)
                    arg.Metadata[item.Key] = item.Value;

            return arg;
        }

        public static implicit operator QueryArguments(ArgumentBuilder builder)
        {
            var args = new QueryArguments();

            var current = builder;
            while (current != null)
            {
                args.Add(current);
                current = current._next;
            }

            return args;
        }
    }
}
