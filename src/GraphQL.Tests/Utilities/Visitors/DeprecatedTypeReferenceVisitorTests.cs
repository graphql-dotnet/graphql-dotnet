using GraphQL.Types;
using GraphQL.Utilities.Visitors.Custom;

namespace GraphQL.Tests.Utilities.Visitors;

public class DeprecatedTypeReferenceVisitorTests
{
    [Fact]
    public void Should_Throw_When_NonDeprecated_Field_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedType = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType.Field<StringGraphType>("id");

        var activeType = new ObjectGraphType { Name = "Post" };
        activeType.Field<StringGraphType>("title");
        // Non-deprecated field referencing deprecated type
        activeType.Field<ObjectGraphType>("author").Type(deprecatedType);

        var schema = new Schema
        {
            Query = activeType
        };
        schema.RegisterType(deprecatedType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated field 'Post.author' references deprecated type 'DeprecatedUser'.");
    }

    [Fact]
    public void Should_Not_Throw_When_Deprecated_Field_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedType = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType.Field<StringGraphType>("id");

        var activeType = new ObjectGraphType { Name = "Post" };
        activeType.Field<StringGraphType>("title");
        // Deprecated field referencing deprecated type - this should be allowed
        activeType.Field<ObjectGraphType>("author")
            .Type(deprecatedType)
            .DeprecationReason("Use newAuthor field instead");

        var schema = new Schema
        {
            Query = activeType
        };
        schema.RegisterType(deprecatedType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        schema.Initialize();
    }

    [Fact]
    public void Should_Throw_AggregateException_When_Multiple_Violations()
    {
        // Arrange
        var deprecatedType1 = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType1.Field<StringGraphType>("id");

        var deprecatedType2 = new ObjectGraphType
        {
            Name = "DeprecatedCategory",
            DeprecationReason = "Use NewCategory instead"
        };
        deprecatedType2.Field<StringGraphType>("name");

        var activeType = new ObjectGraphType { Name = "Post" };
        activeType.Field<StringGraphType>("title");
        activeType.Field<ObjectGraphType>("author").Type(deprecatedType1);
        activeType.Field<ObjectGraphType>("category").Type(deprecatedType2);

        var schema = new Schema
        {
            Query = activeType
        };
        schema.RegisterType(deprecatedType1);
        schema.RegisterType(deprecatedType2);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<AggregateException>(() => schema.Initialize());
        exception.Message.ShouldContain("Schema validation failed. Found multiple non-deprecated fields referencing deprecated types.");
        exception.InnerExceptions.Count.ShouldBe(2);

        var innerMessages = exception.InnerExceptions.Select(ex => ex.Message).ToArray();
        innerMessages.ShouldContain("Non-deprecated field 'Post.author' references deprecated type 'DeprecatedUser'.");
        innerMessages.ShouldContain("Non-deprecated field 'Post.category' references deprecated type 'DeprecatedCategory'.");
    }

    [Fact]
    public void Should_Not_Throw_When_NonDeprecated_Field_References_NonDeprecated_Type()
    {
        // Arrange
        var activeUserType = new ObjectGraphType { Name = "User" };
        activeUserType.Field<StringGraphType>("id");

        var activePostType = new ObjectGraphType { Name = "Post" };
        activePostType.Field<StringGraphType>("title");
        activePostType.Field<ObjectGraphType>("author").Type(activeUserType);

        var schema = new Schema
        {
            Query = activePostType
        };
        schema.RegisterType(activeUserType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        schema.Initialize();
    }

    [Fact]
    public void Should_Throw_When_ObjectField_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedType = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType.Field<StringGraphType>("id");

        var activeType = new ObjectGraphType { Name = "Post" };
        activeType.Field<ObjectGraphType>("author").Type(deprecatedType);

        var schema = new Schema { Query = activeType };
        schema.RegisterType(deprecatedType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated field 'Post.author' references deprecated type 'DeprecatedUser'.");
    }

    [Fact]
    public void Should_Throw_When_InterfaceField_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedType = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType.Field<StringGraphType>("id");

        var activeInterface = new InterfaceGraphType { Name = "IPost" };
        activeInterface.Field<ObjectGraphType>("author").Type(deprecatedType);

        var schema = new Schema { Query = new ObjectGraphType { Name = "Query" } };
        schema.RegisterType(deprecatedType);
        schema.RegisterType(activeInterface);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated field 'IPost.author' references deprecated type 'DeprecatedUser'.");
    }

    [Fact]
    public void Should_Throw_When_InputObjectField_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedInputType = new InputObjectGraphType
        {
            Name = "DeprecatedUserInput",
            DeprecationReason = "Use NewUserInput instead"
        };
        deprecatedInputType.Field<StringGraphType>("name");

        var activeInputType = new InputObjectGraphType { Name = "PostInput" };
        activeInputType.Field<InputObjectGraphType>("author").Type(deprecatedInputType);

        var schema = new Schema { Query = new ObjectGraphType { Name = "Query" } };
        schema.RegisterType(deprecatedInputType);
        schema.RegisterType(activeInputType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated field 'PostInput.author' references deprecated type 'DeprecatedUserInput'.");
    }

    [Fact]
    public void Should_Throw_When_ObjectFieldArgument_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedInputType = new InputObjectGraphType
        {
            Name = "DeprecatedFilterInput",
            DeprecationReason = "Use NewFilterInput instead"
        };
        deprecatedInputType.Field<StringGraphType>("term");

        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("search")
            .Argument(deprecatedInputType, "filter")
            .Resolve(ctx => "result");

        var schema = new Schema { Query = queryType };
        schema.RegisterType(deprecatedInputType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated argument 'Query.search.filter' references deprecated type 'DeprecatedFilterInput'.");
    }

    [Fact]
    public void Should_Throw_When_InterfaceFieldArgument_References_Deprecated_Type()
    {
        // Arrange
        var deprecatedInputType = new InputObjectGraphType
        {
            Name = "DeprecatedSortInput",
            DeprecationReason = "Use NewSortInput instead"
        };
        deprecatedInputType.Field<StringGraphType>("field");

        var activeInterface = new InterfaceGraphType { Name = "ISearchable" };
        activeInterface.Field<StringGraphType>("find")
            .Argument(deprecatedInputType, "sort")
            .Resolve(ctx => "result");

        var schema = new Schema { Query = new ObjectGraphType { Name = "Query" } };
        schema.RegisterType(deprecatedInputType);
        schema.RegisterType(activeInterface);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated argument 'ISearchable.find.sort' references deprecated type 'DeprecatedSortInput'.");
    }

    [Fact]
    public void Should_Handle_Wrapped_Deprecated_Types_In_ObjectFields()
    {
        // Arrange
        var deprecatedType = new ObjectGraphType
        {
            Name = "DeprecatedUser",
            DeprecationReason = "Use NewUser instead"
        };
        deprecatedType.Field<StringGraphType>("id");

        var activeType = new ObjectGraphType { Name = "Post" };
        activeType.Field<NonNullGraphType<ListGraphType<NonNullGraphType<ObjectGraphType>>>>("authors")
            .Type(new NonNullGraphType(new ListGraphType(new NonNullGraphType(deprecatedType))));

        var schema = new Schema { Query = activeType };
        schema.RegisterType(deprecatedType);
        schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exception.Message.ShouldBe("Non-deprecated field 'Post.authors' references deprecated type 'DeprecatedUser'.");
    }
}
