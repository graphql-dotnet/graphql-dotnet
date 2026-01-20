using GraphQL;
using GraphQL.Resolvers;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Droid : ObjectGraphType<Droid>
    {
        public AutoOutputGraphType_Droid()
        {
            // 1. set name from type name
            Name = "Droid";

            // 2. apply graph type attributes (this happens before fields are added)
            {
                var attr = new ImplementsAttribute(typeof(IStarWarsCharacter));
                attr.Modify(this);
            }

            // 3. add fields from IStarWarsCharacter interface
            ConditionalAddField(ConstructField_Id());
            ConditionalAddField(ConstructField_Name());
            // Friends property is marked with [Ignore], so no field is generated
            ConditionalAddField(ConstructField_GetFriends());
            ConditionalAddField(ConstructField_GetFriendsConnection());
            ConditionalAddField(ConstructField_AppearsIn());
            // Cursor property is marked with [Ignore], so no field is generated
            ConditionalAddField(ConstructField_PrimaryFunction());
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

            // 3. configure resolver
            fieldType.Resolver = new FuncFieldResolver<string>(context => ((Droid)context.Source!).Id);

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

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<string>(context => ((Droid)context.Source!).Name);

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
            }

            // 3. process parameters
            var parameters = method.GetParameters();

            // compile arg1
            Func<IResolveFieldContext, StarWarsData> arg1Func;
            {
                var parameter = parameters[0];
                var typeInformation = new TypeInformation(parameter, typeof(StarWarsData), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(StarWarsData), fieldType, typeInformation);

                var arg1_attr1 = new FromServicesAttribute();
                arg1Func = arg1_attr1.GetResolver<StarWarsData>(argInfo);
            }

            // 4. configure resolver
            fieldType.Resolver = new FuncFieldResolver<IEnumerable<IStarWarsCharacter>>(context =>
            {
                var source = (Droid)context.Source!;
                var arg1 = arg1Func(context);
                return source.GetFriends(arg1)!;
            });

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
            }

            // 3. process parameters
            var parameters = method.GetParameters();

            // compile arg1
            Func<IResolveFieldContext, StarWarsData> arg1Func;
            {
                var parameter = parameters[0];
                var typeInformation = new TypeInformation(parameter, typeof(StarWarsData), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(StarWarsData), fieldType, typeInformation);

                var arg1_attr1 = new FromServicesAttribute();
                arg1Func = arg1_attr1.GetResolver<StarWarsData>(argInfo);
            }

            // 4. configure resolver
            fieldType.Resolver = new FuncFieldResolver<Connection<IStarWarsCharacter>>(context =>
            {
                var source = (Droid)context.Source!;
                var arg1 = arg1Func(context);
                return source.GetFriendsConnection(arg1)!;
            });

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

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<Episodes[]>(context => ((Droid)context.Source!).AppearsIn);

            return fieldType;
        }

        public FieldType? ConstructField_PrimaryFunction()
        {
            var fieldType = new FieldType()
            {
                Name = "PrimaryFunction",
                Type = typeof(GraphQLClrOutputTypeReference<string>),
            };

            // configure resolver
            fieldType.Resolver = new FuncFieldResolver<string?>(context => ((Droid)context.Source!).PrimaryFunction);

            return fieldType;
        }
    }
}
