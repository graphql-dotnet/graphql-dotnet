using GraphQL.Types;
using Shouldly;
using System;
using System.ComponentModel;
using System.Linq;
using Xunit;

#pragma warning disable 0618

namespace GraphQL.Tests.Types
{
    public class EnumGraphTypeTests
    {
        [Description("The best colors ever!")]
        [Obsolete("Just some reason")]
        private enum Colors {
            Red = 1,
            Blue,
            Green,

            [Obsolete("No more yellow")]
            Yellow,

            [Description("A pale purple color named after the mallow flower.")]
            Mauve
        }

        class ColorEnum : EnumerationGraphType<Colors>
        {
            public ColorEnum()
            {
                Name = "ColorsEnum";
            }
        }

        class ColorEnumInverseCasing : EnumerationGraphType<Colors>
        {
            public ColorEnumInverseCasing()
            {
                Name = "ColorsEnum";
            }

            protected override string ChangeEnumCase(string val)
            {
                return new string(val.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray());
            }
        }

        private EnumerationGraphType<Colors> type = new EnumerationGraphType<Colors>();

        [Fact]
        public void adds_values_from_enum()
        {
            type.Values.Count().ShouldBe(5);
            type.Values.First().Name.ShouldBe("RED");
        }

        [Fact]
        public void adds_values_from_enum_no_description_attribute()
        {
            type.Values.Count().ShouldBe(5);
            type.Values.First().Description.ShouldBeNull();
        }


        [Fact]
        public void adds_values_from_enum_with_description_attribute()
        {
            type.Values.Count().ShouldBe(5);
            type.Values.Last().Description.ShouldBe("A pale purple color named after the mallow flower.");
        }

        [Fact]
        public void adds_values_from_enum_with_obsolete_attribute()
        {
            type.Values.Count().ShouldBe(5);
            type.Values["YELLOW"].DeprecationReason.ShouldBe("No more yellow");
        }

        [Fact]
        public void description_and_obsolete_from_enum()
        {
            type.Description.ShouldBe("The best colors ever!");
            type.DeprecationReason.ShouldBe("Just some reason");
        }

        [Fact]
        public void adds_values_from_enum_custom_casing()
        {
            type.ParseValue("rED").ShouldBe(Colors.Red);
        }

        [Fact]
        public void infers_name()
        {
            type.Name.ShouldBe("Colors");
        }

        [Fact]
        public void leaves_name_alone()
        {
            var otherType = new ColorEnum();

            otherType.Name.ShouldBe("ColorsEnum");
            otherType.Values.Count().ShouldBe(5);
        }

        [Fact]
        public void parses_from_name()
        {
            type.ParseValue("RED").ShouldBe(Colors.Red);
        }

        [Fact]
        public void parse_value_is_null_safe()
        {
            type.ParseValue(null).ShouldBe(null);
        }

        [Fact]
        public void does_not_allow_nulls_to_be_added()
        {
            Assert.Throws<ArgumentNullException>(() => new EnumerationGraphType().AddValue(null));
        }
    }
}
