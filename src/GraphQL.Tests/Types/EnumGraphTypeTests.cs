using GraphQL.Types;
using Shouldly;
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
            Mauve
        }

        class ColorEnum : EnumerationGraphType<Colors>
        {
            public ColorEnum()
            {
                Name = "ColorsEnum";
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
