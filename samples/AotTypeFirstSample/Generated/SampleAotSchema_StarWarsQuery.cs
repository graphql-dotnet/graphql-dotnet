using GraphQL;
using GraphQL.Resolvers;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_StarWarsQuery : ObjectGraphType<StarWarsQuery>
    {
        public AutoOutputGraphType_StarWarsQuery()
        {
            // 1. apply graph type attributes (this happens before fields are added)

            // set name from attribute
            {
                var attr = new NameAttribute("Query");
                attr.Modify(this);
            }

            // 2. add fields
            ConditionalAddField(ConstructField_hero());
            ConditionalAddField(ConstructField_human());
            ConditionalAddField(ConstructField_droid());
        }

        private void ConditionalAddField(FieldType? fieldType)
        {
            // used when ShouldInclude returns false (note that fields marked with [Ignore] will not generate code at all)
            if (fieldType != null)
                AddField(fieldType);
        }

        public FieldType? ConstructField_hero()
        {
            // 1. setup
            var fieldType = new FieldType()
            {
                Name = "Hero",
                Type = typeof(GraphQLClrOutputTypeReference<IStarWarsCharacter>),
            };
            var method = typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.HeroAsync), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(StarWarsData)])
                ?? throw new InvalidOperationException("Method not found");

            // 2. process attributes on method (none in this case)

            // 3. process parameters
            var parameters = method.GetParameters();

            // compile arg1
            // todo: be able to identify at compile-time if an argument sets an argument resolver or not
            //   for example, [FromServices], [FromSource] and [FromUserContext] always set argument resolvers,
            //   while [Name], [Description], [DefaultValue], etc only modify the argument metadata
            Func<IResolveFieldContext, StarWarsData> arg1Func;
            {
                var parameter = parameters[0];
                // todo: derive from TypeInformation with one that throws an exception if graph type is set
                var typeInformation = new TypeInformation(parameter, typeof(StarWarsData), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(StarWarsData), fieldType, typeInformation);

                var arg1_attr1 = new FromServicesAttribute();
                arg1_attr1.Modify(argInfo); // todo: remove base function that calls the generic version
                arg1_attr1.Modify<StarWarsData>(argInfo);

                // todo: arg1Func = arg1_attr1.ConstructResolver<StarWarsData>(argInfo);
                arg1Func = context => context.RequestServices!.GetRequiredService<StarWarsData>();
            }

            // 4. configure resolver
            // compile resolver (this code is dynamic depending on if it's an async method, etc)
            fieldType.Resolver = new FuncFieldResolver<IStarWarsCharacter>(context =>
            {
                var arg1 = arg1Func(context);
                return new(StarWarsQuery.HeroAsync(arg1)!);
            });

            return fieldType;
        }

        public FieldType? ConstructField_human()
        {
            var fieldType = new FieldType()
            {
                Name = "Human",
                Type = typeof(GraphQLClrOutputTypeReference<Human>),
            };
            var method = typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.HumanAsync), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(StarWarsData), typeof(string)])
                ?? throw new InvalidOperationException("Method not found");
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
            {
                var parameter = parameters[1];
                // since code generation notices that the attribute is derived from InputType, OutputType or ScalarType, it will provide a mutable TypeInformation instance
                var typeInformation = new TypeInformation(parameter, typeof(string), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(string), fieldType, typeInformation);

                var arg2_attr1 = new IdAttribute();
                arg2_attr1.Modify(typeInformation);
                arg2_attr1.Modify(argInfo);
                arg2_attr1.Modify<string>(argInfo);

                var queryArgument = argInfo.ConstructQueryArgument().QueryArgument!;
                arg2_attr1.Modify(queryArgument);

                fieldType.Arguments ??= new();
                fieldType.Arguments.Add(queryArgument);
            }

            // compile resolver (this code is dynamic depending on if it's an async method, etc)
            fieldType.Resolver = new FuncFieldResolver<Human?>(context =>
            {
                var arg1 = arg1Func(context);
                var arg2 = context.GetArgument<string>("id");
                return new(StarWarsQuery.HumanAsync(arg1, arg2)!);
            });

            // apply field arguments - sample for [Scoped] but works for [Name] or similar
            {
                var field_attr1 = new ScopedAttribute();
                field_attr1.Modify(fieldType, false);
            }

            return fieldType;
        }

        public FieldType? ConstructField_droid()
        {
            var fieldType = new FieldType()
            {
                Name = "Droid",
                Type = typeof(GraphQLClrOutputTypeReference<Droid>),
            };
            var method = typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.DroidAsync), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(StarWarsData), typeof(string)])
                ?? throw new InvalidOperationException("Method not found");
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
            {
                var parameter = parameters[1];
                // since code generation notices that the attribute is derived from InputType, OutputType or ScalarType, it will provide a mutable TypeInformation instance
                var typeInformation = new TypeInformation(parameter, typeof(string), false, false, false, null);
                var argInfo = new ArgumentInformation(parameter, typeof(string), fieldType, typeInformation);

                var arg2_attr1 = new IdAttribute();
                arg2_attr1.Modify(typeInformation);
                arg2_attr1.Modify(argInfo);
                arg2_attr1.Modify<string>(argInfo);

                var queryArgument = argInfo.ConstructQueryArgument().QueryArgument!;
                arg2_attr1.Modify(queryArgument);

                fieldType.Arguments ??= new();
                fieldType.Arguments.Add(queryArgument);
            }

            // compile resolver (this code is dynamic depending on if it's an async method, etc)
            fieldType.Resolver = new FuncFieldResolver<Human?>(context =>
            {
                var arg1 = arg1Func(context);
                var arg2 = context.GetArgument<string>("id");
                return new(StarWarsQuery.HumanAsync(arg1, arg2)!);
            });

            // apply field arguments - sample for [Scoped] but works for [Name] or similar
            {
                var field_attr1 = new ScopedAttribute();
                field_attr1.Modify(fieldType, false);
            }

            return fieldType;
        }
    }
}
