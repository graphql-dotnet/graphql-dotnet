﻿using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Builders
{
    public class FieldBuilderTests
    {
        [Test]
        public void should_have_name()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("TheName");

            var fields = objectType.Fields.ToList();
            fields.Count.ShouldEqual(1);
            fields[0].Name.ShouldEqual("TheName");
            fields[0].Description.ShouldEqual(null);
            fields[0].Type.ShouldEqual(typeof(StringGraphType));
        }

        [Test]
        public void should_have_name_and_description()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("TheName")
                .Description("TheDescription");

            var fields = objectType.Fields.ToList();
            fields.Count.ShouldEqual(1);
            fields[0].Name.ShouldEqual("TheName");
            fields[0].Description.ShouldEqual("TheDescription");
            fields[0].Type.ShouldEqual(typeof(StringGraphType));
        }

        [Test]
        public void should_return_the_right_type()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("TheName")
                .Returns<string>()
                .Resolve(_ => "SomeString");

            var fields = objectType.Fields.ToList();
            fields.Count.ShouldEqual(1);
            fields[0].Name.ShouldEqual("TheName");
            fields[0].Type.ShouldEqual(typeof(StringGraphType));

            var context = new ResolveFieldContext();
            fields[0].Resolve(context).GetType().ShouldEqual(typeof(string));
            fields[0].Resolve(context).ShouldEqual("SomeString");
        }

        [Test]
        public void can_be_defined_of_graphtype()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<IntGraphType>()
                .Name("Field1");
            objectType.Field<FloatGraphType>()
                .Name("Field2");
            objectType.Field<DateGraphType>()
                .Name("Field3");

            var fields = objectType.Fields.ToList();
            fields.Count.ShouldEqual(3);
            fields[0].Name.ShouldEqual("Field1");
            fields[0].Type.ShouldEqual(typeof(IntGraphType));
            fields[1].Name.ShouldEqual("Field2");
            fields[1].Type.ShouldEqual(typeof(FloatGraphType));
            fields[2].Name.ShouldEqual("Field3");
            fields[2].Type.ShouldEqual(typeof(DateGraphType));
        }

        [Test]
        public void can_have_a_default_value()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<IntGraphType>()
                .Returns<int>()
                .DefaultValue(15);

            objectType.Fields.First().DefaultValue.ShouldEqual(15);
        }

        [Test]
        public void can_have_arguments_with_and_without_default_values()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<IntGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1", "12345")
                .Argument<IntGraphType, int>("arg2", "desc2", 9)
                .Argument<IntGraphType>("arg3", "desc3");

            var field = objectType.Fields.First();
            field.Arguments.Count.ShouldEqual(3);

            field.Arguments[0].Name.ShouldEqual("arg1");
            field.Arguments[0].Description.ShouldEqual("desc1");
            field.Arguments[0].Type.ShouldEqual(typeof(StringGraphType));
            field.Arguments[0].DefaultValue.ShouldEqual("12345");

            field.Arguments[1].Name.ShouldEqual("arg2");
            field.Arguments[1].Description.ShouldEqual("desc2");
            field.Arguments[1].Type.ShouldEqual(typeof(IntGraphType));
            field.Arguments[1].DefaultValue.ShouldEqual(9);

            field.Arguments[2].Name.ShouldEqual("arg3");
            field.Arguments[2].Description.ShouldEqual("desc3");
            field.Arguments[2].Type.ShouldEqual(typeof(IntGraphType));
            field.Arguments[2].DefaultValue.ShouldEqual(null);
        }

        [Test]
        public void can_determine_whether_argument_exists_in_resolver()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<IntGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1", "12345")
                .Returns<int>()
                .Resolve(context =>
                {
                    context.HasArgument("arg1").ShouldEqual(true);
                    context.HasArgument("arg0").ShouldEqual(false);
                    return 0;
                });

            var field = objectType.Fields.First();
            field.Arguments.Count.ShouldEqual(1);

            field.Arguments[0].Name.ShouldEqual("arg1");
            field.Arguments[0].Description.ShouldEqual("desc1");
            field.Arguments[0].Type.ShouldEqual(typeof(StringGraphType));
            field.Arguments[0].DefaultValue.ShouldEqual("12345");
            field.Resolve(new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>
                {
                    { "arg1", "abc" }
                }
            });
        }

        [Test]
        public void getting_unspecified_argument_in_resolver_yields_null()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1")
                .Resolve(context =>
                {
                    context.GetArgument<string>("arg1").ShouldEqual(null);
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>(),
                FieldDefinition = field,
            });
        }

        [Test]
        public void getting_unspecified_argument_in_resolver_yields_default_value()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1", "default")
                .Resolve(context =>
                {
                    context.GetArgument<string>("arg1").ShouldEqual("default");
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>(),
                FieldDefinition = field,
            });
        }

        [Test]
        public void getting_unspecified_argument_in_resolver_yields_overridden_default_value()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1", "default")
                .Resolve(context =>
                {
                    context.GetArgument("arg1", "default2").ShouldEqual("default2");
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>(),
                FieldDefinition = field,
            });
        }

        [Test]
        public void getting_specified_argument_in_resolver_overrides_default_value()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Argument<StringGraphType, string>("arg1", "desc1", "default")
                .Resolve(context =>
                {
                    context.GetArgument("arg1", "default2").ShouldEqual("arg1value");
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>
                {
                    { "arg1", "arg1value" }
                },
                FieldDefinition = field,
            });
        }

        [Test]
        public void can_access_object()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .WithObject<int>()
                .Resolve(context =>
                {
                    context.Object.ShouldEqual(12345);
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Source = 12345,
            });
        }

        [Test]
        public void can_access_object_with_custom_resolver()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .WithObject(obj => (int)obj + 1)
                .Resolve(context =>
                {
                    context.Object.ShouldEqual(12346);
                    return null;
                });

            var field = objectType.Fields.First();
            field.Resolve(new ResolveFieldContext
            {
                Source = 12345,
            });
        }
    }
}
