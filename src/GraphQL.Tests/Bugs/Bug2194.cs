using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Bugs;

[Collection("StaticTests")]
public class Bug2194
{
    [Fact]
    public void Should_Print_Default_Values_Of_Arguments()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var printer = new SchemaPrinter(new Bug2194Schema(), new SchemaPrinterOptions { IncludeDeprecationReasons = false, IncludeDescriptions = false });
            var printed = printer.Print();
            printed.ShouldBe("Bug2194".ReadSDL());
        });
    }

    public class Bug2194Schema : Schema
    {
        public Bug2194Schema()
        {
            Query = new Bug2194Query();
        }
    }

    public class Bug2194Query : ObjectGraphType
    {
        public Bug2194Query()
        {
            Field<StringGraphType>("test")
                .Argument<NonNullGraphType<GuidGraphType>>("applicationId", arg => arg.DefaultValue = Guid.Parse("A62413D8-41DF-45C5-B228-447F2CD2D368"))
                .Argument<NonNullGraphType<DateGraphType>>("date1", arg => arg.DefaultValue = new DateTime(2021, 03, 06))
                .Argument<NonNullGraphType<DateTimeGraphType>>("date2", arg => arg.DefaultValue = new DateTime(2021, 03, 06, 17, 53, 21))
                .Resolve(_ => "ok!");
        }
    }
}
