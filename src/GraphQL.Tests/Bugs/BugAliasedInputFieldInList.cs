using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class BugAliasedInputFieldInList : QueryTestBase<AliasedInputFieldSchema>
{
    [Fact]
    public void supports_aliased_fields_in_input_list()
    {
        string query = @"mutation { mutateMyEntity(
                singleEntity: { aField: 1 fieldAlias: 2 }
                multipleEntities: [{ aField: 3 fieldAlias: 4 }]
            ) }";

        string expected = @"{ ""mutateMyEntity"": true }";

        AssertQuerySuccess(query, expected, null);
    }
}

public class AliasedInputFieldSchema : Schema
{
    public AliasedInputFieldSchema()
    {
        Mutation = new AliasedInputFieldMutation();
    }
}

public class AliasedInputFieldMutation : ObjectGraphType
{
    public AliasedInputFieldMutation()
    {
        Field<BooleanGraphType>("mutateMyEntity")
            .Argument<MyEntityInputType>("singleEntity")
            .Argument<ListGraphType<MyEntityInputType>>("multipleEntities")
            .Resolve(
                context =>
                {
                    var singleEntity = context.GetArgument<MyEntity>("singleEntity");
                    singleEntity.AField.ShouldBe(1); // <<<<<< This is OK
                    singleEntity.BField.ShouldBe(2); // <<<<<< This is OK

                    var multipleEntities = context.GetArgument<List<MyEntity>>("multipleEntities");
                    multipleEntities[0].AField.ShouldBe(3); // <<<<< This is OK
                    multipleEntities[0].BField.ShouldBe(4); // <<<<< This is fails [BEFORE FIX]

                    return true;
                }
            );
    }
}

public class MyEntityInputType : InputObjectGraphType<MyEntity>
{
    public MyEntityInputType()
    {
        Field(x => x.AField);
        Field("fieldAlias", x => x.BField);
    }
}

public class MyEntity
{
    public int AField { get; set; }
    public int BField { get; set; }
}
