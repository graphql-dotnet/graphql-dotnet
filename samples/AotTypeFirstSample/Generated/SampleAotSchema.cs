using GraphQL;
using GraphQL.Conversion;
using GraphQL.DI;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

// sample schema that would be auto generated in AOT scenarios
public partial class SampleAotSchema : AotSchema, IListConverterFactory
{
    // source-generated constructor (when the developer does not provide one)
    public SampleAotSchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations) : base(services, configurations)
    {
        Configure();
    }

    private void Configure()
    {
        // register constructors for:
        //   1. all source-generated types, including those found during source generation of other types
        //   2. GraphQL built-in types (ID, Int, String, Float, Boolean), except where overridden via [AotRemapType]
        //   3. Any referenced graph types found during source generation, if they have only a default constructor
        //   4. Any graph types generated via [AotInputType] or similar attributes
        //   5. Any graph types generated or referenced via [AotQueryType] or similar attributes, that have only a default constructor
        //   6. Any graph types referenced via [AotTypeMapping] or [AotRemapType] attributes, that have only a default constructor
        AddAotType<AutoOutputGraphType_StarWarsQuery>(); // source-generated type specified for root Query type
        AddAotType<AutoOutputGraphType_IStarWarsCharacter>(); // source-generated type referenced by StarWarsQuery
        AddAotType<EnumerationGraphType<Episodes>>(); // source-generated type referenced by IStarWarsCharacter
        AddAotType<AutoOutputGraphType_Connection_IStarWarsCharacter>(); // source-generated type referenced by IStarWarsCharacter
        AddAotType<AutoOutputGraphType_Edge_IStarWarsCharacter>(); // source-generated type referenced by CharacterConnection
        AddAotType<AutoOutputGraphType_PageInfo>(); // source-generated type referenced by CharacterConnection
        AddAotType<AutoOutputGraphType_Droid>(); // source-generated type referenced by StarWarsQuery
        AddAotType<AutoOutputGraphType_Human>(); // source-generated type referenced by StarWarsQuery
        AddAotType<AutoOutputGraphType_StarWarsMutation>(); // source-generated type specified for root Mutation type
        AddAotType<AutoInputGraphType_HumanInput>(); // source-generated input type referenced by StarWarsMutation
        AddAotType<IntGraphType>(); // built-in type
        AddAotType<StringGraphType>(); // built-in type
        AddAotType<BooleanGraphType>(); // built-in type
        AddAotType<FloatGraphType>(); // built-in type
        AddAotType<IdGraphType>(); // built-in type

        // register types within the schema for:
        //   1. all source-generated types, including those found during source generation of other types
        //   2. GraphQL built-in types (ID, Int, String, Float, Boolean), except where overridden via [AotRemapType]
        //   3. Any referenced graph types found during source generation
        //   4. Any graph types generated via [AotInputType] or similar attributes
        //   5. Any graph types generated or referenced via [AotQueryType] or similar attributes
        //   6. Any graph types referenced via [AotTypeMapping] or [AotRemapType] attributes
        this.RegisterType<AutoOutputGraphType_StarWarsQuery>();
        this.RegisterType<AutoOutputGraphType_IStarWarsCharacter>();
        this.RegisterType<EnumerationGraphType<Episodes>>();
        this.RegisterType<AutoOutputGraphType_Connection_IStarWarsCharacter>();
        this.RegisterType<AutoOutputGraphType_Edge_IStarWarsCharacter>();
        this.RegisterType<AutoOutputGraphType_PageInfo>();
        this.RegisterType<AutoOutputGraphType_Droid>();
        this.RegisterType<AutoOutputGraphType_Human>();
        this.RegisterType<AutoOutputGraphType_StarWarsMutation>();
        this.RegisterType<AutoInputGraphType_HumanInput>();
        this.RegisterType<IntGraphType>();
        this.RegisterType<StringGraphType>();
        this.RegisterType<BooleanGraphType>();
        this.RegisterType<FloatGraphType>();
        this.RegisterType<IdGraphType>();

        // register type mappings for:
        //   1. all source-generated types excluding root types, including those found during source generation of other types
        //   2. Any explicitly mapped types
        //   3. Built-in type mappings
        this.RegisterTypeMapping<IStarWarsCharacter, AutoOutputGraphType_IStarWarsCharacter>();
        this.RegisterTypeMapping<Episodes, EnumerationGraphType<Episodes>>(); // uses AOT-safe reflection during initialization only
        this.RegisterTypeMapping<Connection<IStarWarsCharacter>, AutoOutputGraphType_Connection_IStarWarsCharacter>();
        this.RegisterTypeMapping<Edge<IStarWarsCharacter>, AutoOutputGraphType_Edge_IStarWarsCharacter>();
        this.RegisterTypeMapping<PageInfo, AutoOutputGraphType_PageInfo>();
        this.RegisterTypeMapping<Droid, AutoOutputGraphType_Droid>();
        this.RegisterTypeMapping<Human, AutoOutputGraphType_Human>();
        this.RegisterTypeMapping<HumanInput, AutoInputGraphType_HumanInput>();
        this.RegisterTypeMapping<int, IntGraphType>();
        this.RegisterTypeMapping<string, StringGraphType>();
        this.RegisterTypeMapping<bool, BooleanGraphType>();
        this.RegisterTypeMapping<double, FloatGraphType>();

        // register list conversions for:
        //   1. all input list fields identified during source-generation
        //   2. all input list types specified via [AotListType] attributes
        // (none in this sample)

        // configure root types when specified via [AotQueryType] or similar attributes
        Query = this.GetRequiredService<AutoOutputGraphType_StarWarsQuery>();
        Mutation = this.GetRequiredService<AutoOutputGraphType_StarWarsMutation>();

        // register list types resolving to int[]
        {
            var converter = new ListConverter(typeof(int), values => Array.ConvertAll(values, value => (int?)value ?? default));
            _listConverters[typeof(int[])] = converter;
            _listConverters[typeof(IEnumerable<int>)] = converter;
            _listConverters[typeof(IList<int>)] = converter;
        }
        // register list types resolving to int?[]
        {
            var converter = new ListConverter(typeof(int?), values => Array.ConvertAll(values, value => (int?)value));
            _listConverters[typeof(int?[])] = converter;
            _listConverters[typeof(ICollection<int?>)] = converter;
        }
        // register list types resolving to List<string>
        _listConverters[typeof(List<string>)] = new ListConverter(typeof(string), values => Array.ConvertAll(values, value => (string?)value));

        foreach (var listType in _listConverters.Keys)
        {
            ValueConverter.RegisterListConverterFactory(listType, this);
        }
    }

    private readonly Dictionary<Type, IListConverter> _listConverters = new();
    public IListConverter Create(Type listType)
    {
        return _listConverters[listType];
    }
}
