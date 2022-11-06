using GraphQL.Conversion;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug3331
{
    [Fact]
    public void throws_exception_when_multiple_type_instances_exists_complex()
    {
        var schema = new Schema
        {
            NameConverter = new CamelCaseNameConverter()
        };

        var queryGraphType = new ObjectGraphType
        {
            Name = "Query"
        };

        schema.RegisterType(queryGraphType);

        RegisterGenericQueryGraphTypes(schema, queryGraphType, "MyObject", "MyObjects");

        schema.Query = queryGraphType;

        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("A different instance of the type 'MyObject' has already been registered within the schema. Please use the same instance for all references within the schema, or use GraphQLTypeReference to reference a type instantiated elsewhere.");

        // Must have 2 instances
        MyObjectGraphType.SharedInstanceCounter.ShouldBe(3);
    }

    private static void RegisterGenericQueryGraphTypes(Schema schema, ObjectGraphType queryGraphType,
        string singular, string plural)
    {
        // Important: instance 1
        var objectGraphType = new MyObjectGraphType();
        schema.RegisterType(objectGraphType);

        // Note: we register different types on purpose here (the generic method registers a 2nd instance)
        queryGraphType.Field<MyObjectGraphType>(singular)
            .Arguments(new QueryArguments(
                new QueryArgument(typeof(Guid?).GetGraphTypeFromType(true)) { Name = "Id" }))
            .Resolve(context => new MyObject
            {
                Id = Guid.NewGuid(),
                Name = "test"
            });

        // This uses instance 1
        var objectDataResultGraphType = CreateDataResultObjectGraphType<MyObject>(singular, objectGraphType, null);
        schema.RegisterType(objectDataResultGraphType);

        queryGraphType.Field(plural, objectDataResultGraphType)
            .Arguments(new QueryArguments(
                new QueryArgument(typeof(int?).GetGraphTypeFromType(true)) { Name = "Skip" },
                new QueryArgument(typeof(int?).GetGraphTypeFromType(true)) { Name = "Take" }))
            .Resolve(context =>
            {
                var dataResult = new DataResult<MyObject>
                {
                    Skip = 0,
                    Take = 10,
                    Total = 2
                };

                dataResult.Data.AddRange(new[]
                {
                    new MyObject
                    {
                        Id = Guid.Parse("96b8c7b5-d542-4d70-a0cf-b0bbc0db119f"),
                        Name = "test1"
                    },
                    new MyObject
                    {
                        Id = Guid.Parse("b405cb58-b966-4926-a989-90a76217af66"),
                        Name = "test2"
                    },
                });

                return dataResult;
            });
    }

    private static IObjectGraphType CreateDataResultObjectGraphType<TEntity>(string name, IObjectGraphType objectGraphType, FuncFieldResolver<DataResult<MyObject>, List<TEntity>> fieldResolver)
    {
        var graphType = new ObjectGraphType
        {
            Name = $"{name}DataResult"
        };

        // Total
        graphType.AddField(new FieldType
        {
            Name = nameof(DataResult<MyObject>.Total),
            ResolvedType = new LongGraphType(),
        });

        // Offset
        graphType.AddField(new FieldType
        {
            Name = nameof(DataResult<MyObject>.Skip),
            ResolvedType = new IntGraphType(),
        });

        // Take
        graphType.AddField(new FieldType
        {
            Name = nameof(DataResult<MyObject>.Take),
            ResolvedType = new IntGraphType(),
        });

        // Data
        graphType.AddField(new FieldType
        {
            Name = nameof(DataResult<MyObject>.Data),
            ResolvedType = new ListGraphType(objectGraphType),
            Resolver = fieldResolver
        });

        return graphType;
    }

    public class MyObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class MyObjectGraphType : ObjectGraphType<MyObject>
    {
        public static int SharedInstanceCounter = 1;

        public MyObjectGraphType()
        {
            Field(p => p.Id);
            Field(p => p.Name);

            InstanceCounter = SharedInstanceCounter++;
        }

        public int InstanceCounter { get; }
    }

    public class DataResult<TResult>
    {
        public DataResult()
        {
            Data = new List<TResult>();
        }

        public long Total { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public List<TResult> Data { get; private set; }
    }

    public class DataResultGraphType<TType, TGraphType> : ObjectGraphType<DataResult<TType>>
        where TGraphType : IGraphType
    {
        public DataResultGraphType()
        {
            Field(x => x.Skip);
            Field(x => x.Take);
            Field(x => x.Total);
            Field(x => x.Data);
        }
    }
}
