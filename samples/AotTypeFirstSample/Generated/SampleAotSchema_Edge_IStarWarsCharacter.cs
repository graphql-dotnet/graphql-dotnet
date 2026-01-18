using GraphQL.Resolvers;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Edge_IStarWarsCharacter : ObjectGraphType<Edge<IStarWarsCharacter>>
    {
        public AutoOutputGraphType_Edge_IStarWarsCharacter()
        {
            // 1. apply graph type attributes (this happens before fields are added)

            // set name from type name
            Name = "CharacterEdge";

            // 2. add fields
            ConditionalAddField(ConstructField_Cursor());
            ConditionalAddField(ConstructField_Node());
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_Cursor()
        {
            var fieldType = new FieldType()
            {
                Name = "Cursor",
                Type = typeof(GraphQLClrOutputTypeReference<string>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<string?>(context => ((Edge<IStarWarsCharacter>)context.Source!).Cursor);

            return fieldType;
        }

        public FieldType? ConstructField_Node()
        {
            var fieldType = new FieldType()
            {
                Name = "Node",
                Type = typeof(GraphQLClrOutputTypeReference<IStarWarsCharacter>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<IStarWarsCharacter?>(context => ((Edge<IStarWarsCharacter>)context.Source!).Node);

            return fieldType;
        }
    }
}
