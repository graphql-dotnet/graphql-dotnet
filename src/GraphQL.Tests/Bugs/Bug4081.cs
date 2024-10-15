using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Bug4081
{
    [Fact]
    public void ValidateListConverterErrorMessage()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddSelfActivatingSchema<MySchema>()
            .AddErrorInfoProvider(o => o.ExposeExceptionDetailsMode = GraphQL.Execution.ExposeExceptionDetailsMode.Extensions)
            .AddSystemTextJson()
        );
        var serivces = serviceCollection.BuildServiceProvider();
        var schema = serivces.GetRequiredService<ISchema>();
        var ex = Should.Throw<InvalidOperationException>(schema.Initialize);
        ex.Message.ShouldBe("Failed to compile input object conversion for CLR type 'MyInput2' and graph type 'MyInputType2': Failed to retrieve a list converter for type 'Nullable<MyEnum1>' for the list graph type '[MyEnum1]': Type 'Nullable<MyEnum1>' is not a list type or does not have a compatible public constructor.");
        var inner = ex.InnerException.ShouldNotBeNull();
        inner.Message.ShouldBe("Failed to retrieve a list converter for type 'Nullable<MyEnum1>' for the list graph type '[MyEnum1]': Type 'Nullable<MyEnum1>' is not a list type or does not have a compatible public constructor.");
        inner.InnerException.ShouldNotBeNull().Message.ShouldBe("Type 'Nullable<MyEnum1>' is not a list type or does not have a compatible public constructor.");
    }

    public class MySchema : Schema
    {
        public MySchema(IServiceProvider sp) : base(sp)
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<StringGraphType>("hello2",
                arguments: new QueryArguments(new QueryArgument<MyInputType2> { Name = "input" }),
                resolve: context =>
                {
                    var input = context.GetArgument<MyInput2>("input");
                    var entry = input.Entry;
                    entry.ShouldBe(MyEnum1.Alpha);
                    return "World";
                });

        }
    }

    public class MyInputType2 : InputObjectGraphType<MyInput2>
    {
        public MyInputType2()
        {
            Field<ListGraphType<MyEnumType>>("entry");
        }
    }

    public class MyEnumType : EnumerationGraphType<MyEnum1>
    {
    }

    public class MyInput2
    {
        public MyEnum1? Entry { get; set; }
    }

    public enum MyEnum1
    {
        Alpha,
        Beta
    }
}
