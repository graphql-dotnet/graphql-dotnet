using System.Numerics;
using GraphQL.Types;

namespace GraphQL.Tests;

[Collection("StaticTests")]
public class ObjectExtensionsTests
{
    [Fact]
    public void convert_double_using_cultures()
    {
        CultureTestHelper.UseCultures(convert_double);
    }

    [Fact]
    public void convert_double()
    {
        /* Given */
        const double value = 123.123d;
        var floatType = typeof(double);

        /* When */
        object actual = ValueConverter.ConvertTo(value, floatType);

        /* Then */
        actual.ShouldBe(value);
    }

    [Fact]
    public void convert_decimal_using_cultures()
    {
        CultureTestHelper.UseCultures(convert_decimal);
    }

    [Fact]
    public void convert_decimal()
    {
        /* Given */
        const decimal value = 123.123m;
        var floatType = typeof(decimal);

        /* When */
        object actual = ValueConverter.ConvertTo(value, floatType);

        /* Then */
        actual.ShouldBe(value);
    }

    [Fact]
    public void convert_single_using_cultures()
    {
        CultureTestHelper.UseCultures(convert_single);
    }

    [Fact]
    public void convert_single()
    {
        /* Given */
        const float value = 123.123f;
        var floatType = typeof(float);

        /* When */
        object actual = ValueConverter.ConvertTo(value, floatType);

        /* Then */
        actual.ShouldBe(value);
    }

    [Fact]
    public void convert_double_array_to_array()
    {
        // Arrange
        double[] doubles = new[] { 1.00, 2.01, 3.14 };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double[]), new ListGraphType(new NonNullGraphType(new FloatGraphType())));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_double_list_to_array()
    {
        // Arrange
        var doubles = new List<double> { 1.00, 2.01, 3.14 };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double[]), new ListGraphType(new NonNullGraphType(new FloatGraphType())));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_ndouble_list_to_array()
    {
        // Arrange
        var doubles = new List<double?> { 1.00, 2.01, 3.14, null };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double?[]), new ListGraphType(new FloatGraphType()));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_ndouble_array_to_array()
    {
        // Arrange
        double?[] doubles = new double?[] { 1.00, 2.01, 3.14 };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double?[]), new ListGraphType(new FloatGraphType()));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_double_list_to_list()
    {
        // Arrange
        var doubles = new List<double> { 1.00, 2.01, 3.14 };

        // Act
        object actual = doubles.GetPropertyValue(typeof(List<double>), new ListGraphType(new NonNullGraphType(new FloatGraphType())));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_double_list_of_arrays_to_list_of_arrays()
    {
        // Arrange
        var doubles = new List<double[]> { new[] { 1.00, 2.01, 3.14 }, new[] { 3.25, 2.21, 1.10 } };

        // Act
        object actual = doubles.GetPropertyValue(typeof(List<double[]>), new ListGraphType(new ListGraphType(new NonNullGraphType(new FloatGraphType()))));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_double_array_of_arrays_to_array_of_arrays()
    {
        // Arrange
        double[][] doubles = new[] { new[] { 1.00, 2.01, 3.14 }, new[] { 3.25, 2.21, 1.10 } };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double[][]), new ListGraphType(new ListGraphType(new NonNullGraphType(new FloatGraphType()))));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_double_list_of_arrays_to_array_of_arrays()
    {
        // Arrange
        var doubles = new List<double[]> { new[] { 1.00, 2.01, 3.14 }, new[] { 3.25, 2.21, 1.10 } };

        // Act
        object actual = doubles.GetPropertyValue(typeof(double[][]), new ListGraphType(new ListGraphType(new NonNullGraphType(new FloatGraphType()))));

        // Assert
        actual.ShouldBe(doubles);
    }

    [Fact]
    public void convert_strings_array_to_array()
    {
        // Arrange
        string[] strings = new[] { "foo", "bar", "new" };

        // Act
        object actual = strings.GetPropertyValue(typeof(string[]), new ListGraphType(new NonNullGraphType(new StringGraphType())));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_strings_list_to_array()
    {
        // Arrange
        var strings = new List<string> { "foo", "bar", "new" };

        // Act
        object actual = strings.GetPropertyValue(typeof(string[]), new ListGraphType(new NonNullGraphType(new StringGraphType())));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_strings_list_to_list()
    {
        // Arrange
        var strings = new List<string> { "foo", "bar", "new" };

        // Act
        object actual = strings.GetPropertyValue(typeof(List<string>), new ListGraphType(new NonNullGraphType(new StringGraphType())));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_string_list_of_arrays_to_list_of_arrays()
    {
        // Arrange
        var strings = new List<string[]> { new[] { "foo", "bar", "boo" }, new[] { "new", "year", "eve" } };

        // Act
        object actual = strings.GetPropertyValue(typeof(List<string[]>), new ListGraphType(new NonNullGraphType(new StringGraphType())));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_string_array_of_arrays_to_array_of_arrays()
    {
        // Arrange
        string[][] strings = new[] { new[] { "foo", "bar", "boo" }, new[] { "new", "year", "eve" } };

        // Act
        object actual = strings.GetPropertyValue(typeof(string[][]), new ListGraphType(new ListGraphType(new NonNullGraphType(new StringGraphType()))));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_string_list_of_arrays_to_array_of_arrays()
    {
        // Arrange
        var strings = new List<string[]> { new[] { "foo", "bar", "boo" }, new[] { "new", "year", "eve" } };

        // Act
        object actual = strings.GetPropertyValue(typeof(string[][]), new ListGraphType(new ListGraphType(new NonNullGraphType(new StringGraphType()))));

        // Assert
        actual.ShouldBe(strings);
    }

    [Fact]
    public void convert_string_list_of_lists_to_array_of_arrays()
    {
        // Arrange
        var strings = new List<List<string>> { new List<string> { "foo", "bar", "boo" }, new List<string> { "new", "year", "eve" } };

        // Act
        object actual = strings.GetPropertyValue(typeof(string[][]), new ListGraphType(new ListGraphType(new NonNullGraphType(new StringGraphType()))));

        // Assert
        actual.ShouldBe(strings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_uses_public_default_constructor_when_available(bool compile)
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput1>(compile);
        person.Name.ShouldBe("tom");
        person.Age.ShouldBe(10);
    }

    private class MyInput1
    {
        public string Name { get; set; }
        public int Age { get; set; }
        // carefully selected ordering of constructors
        public MyInput1(string name) { throw new InvalidOperationException(); }
        public MyInput1() { }
        public MyInput1(string name, int age) { throw new InvalidOperationException(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_ignores_private_constructors(bool compile)
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput2>(compile);
        person.Name.ShouldBe("tom");
        person.Age.ShouldBe(10);
    }

    private class MyInput2
    {
        public string Name { get; set; }
        public int Age { get; set; }
        private MyInput2() { throw new InvalidOperationException(); }
        public MyInput2(string name, int age) { Name = name; Age = age; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_throws_for_multiple_constructors(bool compile)
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        Should.Throw<InvalidOperationException>(() => inputs.ToObject<MyInput3>(compile));
    }

    private class MyInput3
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public MyInput3(string name) { Name = name; }
        public MyInput3(string name, int age) { Name = name; Age = age; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_honors_marked_constructor(bool compile)
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput4>(compile);
        person.Name.ShouldBe("tom");
        person.Age.ShouldBe(10);
    }

    private class MyInput4
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public MyInput4(string name, int age) { throw new InvalidOperationException(); }
        [GraphQLConstructor]
        public MyInput4(string name) { Name = name; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_sets_initonly_props(bool compile)
    {
        var inputs = """{ "name": "tom" }""".ToInputs();
        var person = inputs.ToObject<MyInput5>(compile);
        person.Name.ShouldBe("tom");
    }

    private class MyInput5
    {
        public string Name { get; init; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_initializes_initonly_props(bool compile)
    {
        var inputs = """{ "company": "test", "month": 5 }""".ToInputs();
        var person = inputs.ToObject<MyInput6>(compile);
        person.Name.ShouldBe(null);
        person.Company.ShouldBe("test");
        person.Description.ShouldBe("def");
        person.Age.ShouldBe(0);
        person.Month.ShouldBe(5);
        person.Year.ShouldBe(-3);
    }

    private class MyInput6
    {
        public string Name { get; init; } = "abc";
        public string Company { get; init; } = "ghi";
        public string Description { get; set; } = "def";
        public int Age { get; init; } = -1;
        public int Month { get; init; } = -2;
        public int Year { get; set; } = -3;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_initializes_required_props(bool compile)
    {
        var inputs = """{ "company": "test", "month": 5 }""".ToInputs();
        var person = inputs.ToObject<MyInput7>(compile);
        person.Name.ShouldBe(null);
        person.Company.ShouldBe("test");
        person.Description.ShouldBe("def");
        person.Age.ShouldBe(0);
        person.Month.ShouldBe(5);
        person.Year.ShouldBe(-3);
    }

    private class MyInput7
    {
        public required string Name { get; set; } = "abc";
        public required string Company { get; set; } = "ghi";
        public string Description { get; set; } = "def";
        public required int Age { get; set; } = -1;
        public required int Month { get; set; } = -2;
        public int Year { get; set; } = -3;
    }

    [Fact]
    public void toobject_cannot_initialize_readonly_field()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema()
        {
            Query = queryObject
        };
        var inputType = new MyInput8Type();
        schema.RegisterType(inputType);

        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("Field named 'Age' on CLR type 'MyInput8' is defined as a read-only field. Please add a constructor parameter with the same name to initialize this field.");
    }

    private class MyInput8Type : InputObjectGraphType<MyInput8>
    {
        public MyInput8Type()
        {
            Field(x => x.Name);
            Field(x => x.Age);
        }
    }

    private class MyInput8
    {
        public required string Name { get; set; } = "abc";
        public readonly int Age = -1;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void toobject_uses_valueconverter(bool compile, bool withValueConverter)
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        if (withValueConverter)
            ValueConverter.Register(_ => new MyInput9("testing"));
        try
        {
            if (withValueConverter)
            {
                inputs.ToObject<MyInput9>(compile).ShouldNotBeNull();
            }
            else
            {
                Should.Throw<InvalidOperationException>(() => inputs.ToObject<MyInput9>(compile));
            }
        }
        finally
        {
            ValueConverter.Register<MyInput9>(null);
        }
    }

    private class MyInput9
    {
        public MyInput9(string dummy) { _ = dummy; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_uses_constructor_with_optional_parameters(bool compile)
    {
        var inputs = """{ "name": "tom" }""".ToInputs();
        var value = inputs.ToObject<MyInput10>(compile).ShouldNotBeNull();
        value.Name.ShouldBe("tom");
    }

    public class MyInput10
    {
        public MyInput10(string name, int age = 10)
        {
            Name = name;
            age.ShouldBe(10);
        }

        public string Name { get; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_throws_for_constructor_with_unknown_parameters(bool compile)
    {
        var inputs = """{ "name": "tom" }""".ToInputs();
        Should.Throw<InvalidOperationException>(() => inputs.ToObject<MyInput11>(compile))
            .Message.ShouldBe("Cannot find field named 'age' on graph type 'MyInput11' to fulfill constructor parameter for CLR type 'MyInput11'.");
    }

    public class MyInput11
    {
        public MyInput11(string name, int age)
        {
            Name = name;
            _ = age;
        }

        public string Name { get; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_works_for_fields(bool compile)
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema()
        {
            Query = queryObject
        };
        var inputType = new InputObjectGraphType<MyInput12>();
        inputType.Field(x => x.Name);
        schema.RegisterType(inputType);
        schema.Initialize();

        var inputs = """{ "name": "tom" }""".ToInputs();
        object value;
        if (compile)
        {
            value = GraphQL.ObjectExtensions.CompileToObject(typeof(MyInput12), inputType)(inputs);
        }
        else
        {
            value = inputs.ToObject(typeof(MyInput12), inputType);
        }
        value.ShouldBeOfType<MyInput12>().Name.ShouldBe("tom");
    }

    public class MyInput12
    {
        public string Name;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_throws_for_invalid_list_type(bool compiled)
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema()
        {
            Query = queryObject
        };
        var inputType = new InputObjectGraphType<MyInput12>();
        inputType.Field(x => x.Name, type: typeof(NonNullGraphType<ListGraphType<IntGraphType>>));
        schema.RegisterType(inputType);
        if (compiled)
        {
            Should.Throw<InvalidOperationException>(() => schema.Initialize())
                .Message.ShouldBe("Could not determine enumerable type for CLR type 'String' while coercing graph type '[Int]'.");
        }
        else
        {
            GlobalSwitches.DynamicallyCompileToObject = false;
            try
            {
                schema.Initialize();
                Should.Throw<InvalidOperationException>(() => inputType.ParseDictionary("""{ "name": [1,2,3] }""".ToInputs()));
            }
            finally
            {
                GlobalSwitches.DynamicallyCompileToObject = true;
            }
        }
    }

    public class MyInput13
    {
        public string Name;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void toobject_works_nested_complex_objects(bool compile)
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema()
        {
            Query = queryObject
        };
        var inputType15 = new InputObjectGraphType() { Name = "Input15" };
        inputType15.Field<StringGraphType>("Name");
        var inputType = new InputObjectGraphType() { Name = "Input14" };
        inputType.Field("Person", inputType15);
        schema.RegisterType(inputType);
        schema.Initialize();

        var inputs = """{ "person": { "name": "tom" } }""".ToInputs();
        object value;
        if (compile)
        {
            value = GraphQL.ObjectExtensions.CompileToObject(typeof(MyInput14), inputType)(inputs);
        }
        else
        {
            value = inputs.ToObject(typeof(MyInput14), inputType);
        }
        value.ShouldBeOfType<MyInput14>().Person.ShouldNotBeNull().Name.ShouldBe("tom");
    }

    public class MyInput14
    {
        public MyInput15 Person { get; set; }
    }

    public class MyInput15
    {
        public string Name { get; set; }
    }
}
