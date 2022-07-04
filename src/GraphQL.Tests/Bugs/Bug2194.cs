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
            Field<StringGraphType>(
                "test",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>>
                    {
                        Name = "applicationId",
                        DefaultValue = Guid.Parse("A62413D8-41DF-45C5-B228-447F2CD2D368")
                    },
                    new QueryArgument<NonNullGraphType<DateGraphType>>
                    {
                        Name = "date1",
                        DefaultValue = new DateTime(2021, 03, 06)
                    },
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>>
                    {
                        Name = "date2",
                        DefaultValue = new DateTime(2021, 03, 06, 17, 53, 21),
                    }),
                resolve: _ => "ok!");
        }
    }
}
