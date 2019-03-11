using GraphQL.Types;
using Shouldly;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class EnumGraphTypeTests
    {
        private enum Colors {
            Red = 1,
            Blue,
            Green,
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
    }
}
