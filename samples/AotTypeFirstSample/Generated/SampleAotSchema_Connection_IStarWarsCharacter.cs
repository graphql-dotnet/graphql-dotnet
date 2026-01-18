using GraphQL.Resolvers;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Connection_IStarWarsCharacter : ObjectGraphType<Connection<IStarWarsCharacter>>
    {
        public AutoOutputGraphType_Connection_IStarWarsCharacter()
        {
            // 1. apply graph type attributes (this happens before fields are added)

            // set name from type name
            Name = "CharacterConnection";

            // 2. add fields
            ConditionalAddField(ConstructField_TotalCount());
            ConditionalAddField(ConstructField_PageInfo());
            ConditionalAddField(ConstructField_Edges());
            ConditionalAddField(ConstructField_Items());
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_TotalCount()
        {
            var fieldType = new FieldType()
            {
                Name = "TotalCount",
                Type = typeof(GraphQLClrOutputTypeReference<int>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<int?>(context => ((Connection<IStarWarsCharacter>)context.Source!).TotalCount);

            return fieldType;
        }

        public FieldType? ConstructField_PageInfo()
        {
            var fieldType = new FieldType()
            {
                Name = "PageInfo",
                Type = typeof(GraphQLClrOutputTypeReference<PageInfo>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<PageInfo?>(context => ((Connection<IStarWarsCharacter>)context.Source!).PageInfo);

            return fieldType;
        }

        public FieldType? ConstructField_Edges()
        {
            var fieldType = new FieldType()
            {
                Name = "Edges",
                Type = typeof(ListGraphType<GraphQLClrOutputTypeReference<Edge<IStarWarsCharacter>>>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<List<Edge<IStarWarsCharacter>>?>(context => ((Connection<IStarWarsCharacter>)context.Source!).Edges);

            return fieldType;
        }

        public FieldType? ConstructField_Items()
        {
            var fieldType = new FieldType()
            {
                Name = "Items",
                Type = typeof(ListGraphType<GraphQLClrOutputTypeReference<IStarWarsCharacter>>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<List<IStarWarsCharacter?>?>(context => ((Connection<IStarWarsCharacter>)context.Source!).Items);

            return fieldType;
        }
    }
}
