using GraphQL.StarWars.Types;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ComplexGraphTypeTests
    {
        internal class ComplexType<T> : ComplexGraphType<T> { }


        [Fact]
        public void accepts_property_expressions()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field(d => d.Name);

            type.Fields.Last().Name.ShouldBe("name");
            type.Fields.Last().Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
        }


        [Fact]
        public void allows_custom_name()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field(d => d.Name)
                .Name("droid");

            type.Fields.Last().Name.ShouldBe("droid");
        }

        [Fact]
        public void infers_nullable_types()
        {
            var type = new ComplexType<Droid>();

            type.Field("appearsIn", d => d.AppearsIn.First(), nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
        }

        [Fact]
        public void throws_when_name_is_not_inferable()
        {
            var type = new ComplexType<Droid>();

            Should.Throw<ArgumentException>(() =>
                type.Field(d => d.AppearsIn.First())
            );
        }

        [Fact]
        public void create_field_with_func_resolver()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field<StringGraphType>("name",
                resolve: context => context.Source.Name
            );

            type.Fields.Last().Type.ShouldBe(typeof(StringGraphType));
        }
    }
}
