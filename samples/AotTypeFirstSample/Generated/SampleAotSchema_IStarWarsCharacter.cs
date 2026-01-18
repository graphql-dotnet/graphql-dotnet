using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_IStarWarsCharacter : InterfaceGraphType<IStarWarsCharacter>
    {
        public AutoOutputGraphType_IStarWarsCharacter()
        {
            // 1. set name from type
            Name = "IStarWarsCharacter";

            // 2. apply graph type attributes (this happens before fields are added)
            {
                var attr = new NameAttribute("Character");
                attr.Modify(this);
            }

            // 3. add fields
            ConditionalAddField(ConstructField_Id());
            ConditionalAddField(ConstructField_Name());
            // Friends property is marked with [Ignore], so no field is generated
            ConditionalAddField(ConstructField_GetFriends());
            ConditionalAddField(ConstructField_GetFriendsConnection());
            ConditionalAddField(ConstructField_AppearsIn());
            // Cursor property is marked with [Ignore], so no field is generated
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_Id()
        {
            // 1. setup
            var fieldType = new FieldType()
            {
                Name = "Id",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>),
            };

            // 2. process attributes on property
            {
                var attr = new System.ComponentModel.DescriptionAttribute("The id of the character.");
                fieldType.Description = attr.Description;
            }

            return fieldType;
        }

        public FieldType? ConstructField_Name()
        {
            var fieldType = new FieldType()
            {
                Name = "Name",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>),
            };

            // process attributes on property
            {
                var attr = new System.ComponentModel.DescriptionAttribute("The name of the character.");
                fieldType.Description = attr.Description;
            }

            return fieldType;
        }

        public FieldType? ConstructField_GetFriends()
        {
            // 1. setup
            var fieldType = new FieldType()
            {
                Name = "GetFriends",
                Type = typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<IStarWarsCharacter>>>),
            };
            var method = typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriends))
                ?? throw new InvalidOperationException("Method not found");

            // 2. process attributes on method
            {
                var attr = new NameAttribute("Friends");
                attr.Modify(fieldType, false);
                if (!attr.ShouldInclude(method, false))
                    return null;
            }

            // 3. process parameters excluding those that are argument resolvers
            var parameters = method.GetParameters();

            return fieldType;
        }

        public FieldType? ConstructField_GetFriendsConnection()
        {
            // 1. setup
            var fieldType = new FieldType()
            {
                Name = "GetFriendsConnection",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Connection<IStarWarsCharacter>>>),
            };
            var method = typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriendsConnection))
                ?? throw new InvalidOperationException("Method not found");

            // 2. process attributes on method
            {
                var attr = new NameAttribute("FriendsConnection");
                attr.Modify(fieldType, false);
                if (!attr.ShouldInclude(method, false))
                    return null;
            }

            // 3. process parameters excluding those that are argument resolvers
            var parameters = method.GetParameters();

            return fieldType;
        }

        public FieldType? ConstructField_AppearsIn()
        {
            var fieldType = new FieldType()
            {
                Name = "AppearsIn",
                Type = typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<Episodes>>>),
            };

            // process attributes on property
            {
                var attr = new System.ComponentModel.DescriptionAttribute("Which movie they appear in.");
                fieldType.Description = attr.Description;
            }

            return fieldType;
        }
    }
}
