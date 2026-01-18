using GraphQL;
using GraphQL.Resolvers;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_StarWarsMutation : ObjectGraphType<StarWarsMutation>
    {
        public AutoOutputGraphType_StarWarsMutation()
        {
            // 1. apply graph type attributes (this happens before fields are added)

            // set name from attribute
            {
                var attr = new NameAttribute("Mutation");
                attr.Modify(this);
            }

            // 2. add fields
            ConditionalAddField(ConstructField_CreateHuman());
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_CreateHuman()
        {
            // 1. setup
            var fieldType = new FieldType()
            {
                Name = "CreateHuman",
                Type = typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Human>>),
            };
            var method = typeof(StarWarsMutation).GetMethod(nameof(StarWarsMutation.CreateHuman), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(StarWarsData), typeof(HumanInput)])
                ?? throw new InvalidOperationException("Method not found");

            // 2. process attributes on method (none in this case)

            // 3. process parameters
            var parameters = method.GetParameters();

            // compile arg1
            Func<IResolveFieldContext, StarWarsData> arg1Func;
            {
                var parameter = parameters[0];
                var typeInformation = new TypeInformation(parameter, typeof(StarWarsData), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(StarWarsData), fieldType, typeInformation);

                var arg1_attr1 = new FromServicesAttribute();
                arg1_attr1.Modify(typeInformation);
                arg1_attr1.Modify(argInfo);
                arg1_attr1.Modify<StarWarsData>(argInfo);

                arg1Func = context => context.RequestServices!.GetRequiredService<StarWarsData>();
            }

            // create query argument for arg2
            QueryArgument queryArgument2;
            {
                var parameter = parameters[1];
                var typeInformation = new TypeInformation(parameter, typeof(HumanInput), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(HumanInput), fieldType, typeInformation);

                queryArgument2 = argInfo.ConstructQueryArgument().QueryArgument!;

                fieldType.Arguments ??= new();
                fieldType.Arguments.Add(queryArgument2);
            }

            // 4. configure resolver
            fieldType.Resolver = new FuncFieldResolver<Human>(context =>
            {
                var arg1 = arg1Func(context);
                var arg2 = context.GetArgument<HumanInput>(queryArgument2.Name);
                return StarWarsMutation.CreateHuman(arg1, arg2)!;
            });

            return fieldType;
        }
    }
}
