using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_PageInfo : ObjectGraphType<PageInfo>
    {
        public AutoOutputGraphType_PageInfo()
        {
            // 1. apply graph type attributes (this happens before fields are added)

            // set name from type name
            Name = "PageInfo";

            // 2. add fields
            ConditionalAddField(ConstructField_HasNextPage());
            ConditionalAddField(ConstructField_HasPreviousPage());
            ConditionalAddField(ConstructField_StartCursor());
            ConditionalAddField(ConstructField_EndCursor());
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_HasNextPage()
        {
            var fieldType = new FieldType()
            {
                Name = "HasNextPage",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<bool>>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<bool>(context => ((PageInfo)context.Source!).HasNextPage);

            return fieldType;
        }

        public FieldType? ConstructField_HasPreviousPage()
        {
            var fieldType = new FieldType()
            {
                Name = "HasPreviousPage",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<bool>>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<bool>(context => ((PageInfo)context.Source!).HasPreviousPage);

            return fieldType;
        }

        public FieldType? ConstructField_StartCursor()
        {
            var fieldType = new FieldType()
            {
                Name = "StartCursor",
                Type = typeof(GraphQLClrOutputTypeReference<string>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<string?>(context => ((PageInfo)context.Source!).StartCursor);

            return fieldType;
        }

        public FieldType? ConstructField_EndCursor()
        {
            var fieldType = new FieldType()
            {
                Name = "EndCursor",
                Type = typeof(GraphQLClrOutputTypeReference<string>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<string?>(context => ((PageInfo)context.Source!).EndCursor);

            return fieldType;
        }
    }
}
