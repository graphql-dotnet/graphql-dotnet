using GraphQL.Conversion;
using GraphQL.DI;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.Tests.Types.Collections;

public class SchemaTypesTests
{
    [Fact]
    public void does_not_request_instance_more_than_once()
    {
        // configure DI provider
        var services = new ServiceCollection();
        services.AddSingleton<StarWarsData>();
        services.AddSingleton<StarWarsQuery>();
        services.AddSingleton<StarWarsMutation>();
        services.AddSingleton<HumanType>();
        services.AddSingleton<HumanInputType>();
        services.AddSingleton<DroidType>();
        services.AddSingleton<CharacterInterface>();
        services.AddSingleton<EpisodeEnum>();
        services.AddSingleton<PageInfoType>();
        services.AddSingleton(typeof(ConnectionType<>));
        services.AddSingleton(typeof(ConnectionType<,>));
        services.AddSingleton(typeof(EdgeType<>));
        using var provider = services.BuildServiceProvider();

        // mock it so we can verify behavior
        var mock = new Mock<IServiceProvider>(MockBehavior.Loose);
        mock.Setup(x => x.GetService(It.IsAny<Type>())).Returns<Type>(type => provider.GetService(type));

        // run test
        var schema = new StarWarsSchema(mock.Object);
        schema.Initialize();

        // verify that GetService was only called once for each schema type
        mock.Verify(x => x.GetService(typeof(StarWarsQuery)), Times.Once);
        mock.Verify(x => x.GetService(typeof(StarWarsMutation)), Times.Once);
        mock.Verify(x => x.GetService(typeof(CharacterInterface)), Times.Once);
        mock.Verify(x => x.GetService(typeof(DroidType)), Times.Once);
        mock.Verify(x => x.GetService(typeof(HumanInputType)), Times.Once);
        mock.Verify(x => x.GetService(typeof(HumanType)), Times.Once);
        mock.Verify(x => x.GetService(typeof(EpisodeEnum)), Times.Once);
        mock.Verify(x => x.GetService(typeof(IEnumerable<IConfigureSchema>)), Times.Once);
        mock.Verify(x => x.GetService(typeof(IEnumerable<IGraphTypeMappingProvider>)), Times.Once);
        mock.Verify(x => x.GetService(typeof(PageInfoType)), Times.Once);
        mock.Verify(x => x.GetService(typeof(ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>)), Times.Once);
        mock.Verify(x => x.GetService(typeof(EdgeType<CharacterInterface>)), Times.Once);
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public void throws_exception_when_multiple_type_instances_exists()
    {
        var schema = new Schema
        {
            NameConverter = new CamelCaseNameConverter()
        };

        var queryGraphType = new ObjectGraphType
        {
            Name = "Query"
        };

        schema.Query = queryGraphType;

        // Object 1
        var graphType1 = new ObjectGraphType
        {
            Name = "MyObject"
        };

        graphType1.Field<IntGraphType>("int");

        queryGraphType.Field("first", graphType1);

        // Object 2
        var graphType2 = new ObjectGraphType
        {
            Name = "MyObject"
        };

        graphType2.Field<IntGraphType>("int");
        graphType2.Field<StringGraphType>("string");

        queryGraphType.Field("second", graphType2);

        // Test
        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe(@"A different instance of the GraphType 'ObjectGraphType' with the name 'MyObject' has already been registered within the schema. Please use the same instance for all references within the schema, or use GraphQLTypeReference to reference a type instantiated elsewhere.
To view additional trace enable GlobalSwitches.TrackGraphTypeInitialization switch.");
    }

    [Fact]
    public void cannot_call_initialize_more_than_once()
    {
        _ = new SchemaTypes_Test_Cannot_Initialize_More_Than_Once();
    }

    private class SchemaTypes_Test_Cannot_Initialize_More_Than_Once : SchemaTypes
    {
        public SchemaTypes_Test_Cannot_Initialize_More_Than_Once()
        {
            var serviceProvider = new DefaultServiceProvider();
            var schema = new Schema(serviceProvider)
            {
                Query = new ObjectGraphType()
            };
            schema.Query.AddField(new FieldType { Name = "field1", Type = typeof(IntGraphType) });
            Initialize(schema, serviceProvider, null);
            Should.Throw<InvalidOperationException>(() => Initialize(schema, serviceProvider, null));
        }
    }

    [Fact]
    public void can_initialize_built_in_types()
    {
        // create a service provider which returns null for all requested services
        var mockServiceProvider = Mock.Of<IServiceProvider>(MockBehavior.Loose);

        // create a schema for the built-in types
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>("string");
        queryType.Field<IntGraphType>("int");
        queryType.Field<IdGraphType>("id");
        queryType.Field<BooleanGraphType>("boolean");
        queryType.Field<FloatGraphType>("float");
        var schema = new Schema(mockServiceProvider)
        {
            Query = queryType
        };

        // attempt to initialize the schema
        schema.Initialize();
    }

    [Fact]
    public void can_initialize_built_in_custom_types()
    {
        // create a service provider which returns null for all requested services
        var mockServiceProvider = Mock.Of<IServiceProvider>(MockBehavior.Loose);

        // create a schema for the built-in types
        var queryType = new ObjectGraphType();

        // date/time types
        queryType.Field<DateTimeGraphType>("datetime");
        queryType.Field<DateTimeOffsetGraphType>("datetimeoffset");
        queryType.Field<DateGraphType>("date");
        queryType.Field<TimeSpanSecondsGraphType>("timespanseconds");
        queryType.Field<TimeSpanMillisecondsGraphType>("timespanmilliseconds");
#if NET6_0_OR_GREATER
        queryType.Field<DateOnlyGraphType>("dateonly");
        queryType.Field<TimeOnlyGraphType>("timeonly");
#endif

        // floating-point types
        queryType.Field<DecimalGraphType>("decimal");
#if NET5_0_OR_GREATER
        queryType.Field<HalfGraphType>("half");
#endif

        // integer types
        queryType.Field<BigIntGraphType>("bigint");
        queryType.Field<UIntGraphType>("uint");
        queryType.Field<LongGraphType>("long");
        queryType.Field<ULongGraphType>("ulong");
        queryType.Field<ShortGraphType>("short");
        queryType.Field<UShortGraphType>("ushort");
        queryType.Field<ByteGraphType>("byte");
        queryType.Field<SByteGraphType>("sbyte");

        // other types
        queryType.Field<UriGraphType>("uri");
        queryType.Field<GuidGraphType>("guid");

        var schema = new Schema(mockServiceProvider)
        {
            Query = queryType
        };

        // attempt to initialize the schema
        schema.Initialize();
    }
}
