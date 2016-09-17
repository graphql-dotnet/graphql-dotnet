﻿using System;
using GraphQL.Types;
using GraphQL.Resolvers;
using System.Linq.Expressions;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(IGraphType type = null)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }
    }

    public class FieldBuilder<TSourceType, TReturnType>
    {

        private FieldType _fieldType { get; set; }

        public FieldType FieldType => _fieldType;

        private FieldBuilder(FieldType fieldType)
        {
            _fieldType = fieldType;
        }

        public static FieldBuilder<TSourceType, TReturnType> Create(IGraphType type = null)
        {
            var fieldType = new FieldType
            {
                Type = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
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

        public FieldBuilder<TSourceType, TReturnType> Argument(IGraphType type, string name, string description)
        {
            _fieldType.Arguments.Add(new QueryArgument(type)
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentType>(
            string name, 
            string description,
            TArgumentType defaultValue = default(TArgumentType)
        ) {
            return Argument(
                type: typeof(TArgumentType).GetGraphTypeFromType(),
                name: name,
                description: description
            );
        }
    }
}
