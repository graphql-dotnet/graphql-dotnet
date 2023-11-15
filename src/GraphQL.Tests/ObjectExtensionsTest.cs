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

    [Fact]
    public void toobject_uses_public_default_constructor_when_available()
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput1>();
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

    [Fact]
    public void toobject_ignores_private_constructors()
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput2>();
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

    [Fact]
    public void toobject_throws_for_multiple_constructors()
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        Should.Throw<InvalidOperationException>(inputs.ToObject<MyInput3>);
    }

    private class MyInput3
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public MyInput3(string name) { Name = name; }
        public MyInput3(string name, int age) { Name = name; Age = age; }
    }

    [Fact]
    public void toobject_honors_marked_constructor()
    {
        var inputs = """{ "name": "tom", "age": 10 }""".ToInputs();
        var person = inputs.ToObject<MyInput4>();
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
}
