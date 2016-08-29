using System;
using GraphQL.Types;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TGraphType, TSourceType, object> Create<TGraphType, TSourceType>()
            where TGraphType : GraphType
        {
            return FieldBuilder<TGraphType, TSourceType, object>.Create();
        }
    }

    public class FieldBuilder<TGraphType, TSourceType, TReturnType>
        where TGraphType : GraphType
    {

        public FieldType FieldType { get; protected set; }

        private FieldBuilder(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        public static FieldBuilder<TGraphType, TSourceType, TReturnType> Create()
        {
            var fieldType = new FieldType
            {
                Type = typeof(TGraphType),
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TGraphType, TSourceType, TReturnType>(fieldType);
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> DefaultValue(TReturnType defaultValue = default(TReturnType))
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        public FieldBuilder<TGraphType, TSourceType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new FieldBuilder<TGraphType, TSourceType, TNewReturnType>(FieldType);
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string description)
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public FieldBuilder<TGraphType, TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
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

        public void Resolve(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            FieldType.Resolve = context => resolver(new ResolveFieldContext<TSourceType>(context));
        }
    }
}
