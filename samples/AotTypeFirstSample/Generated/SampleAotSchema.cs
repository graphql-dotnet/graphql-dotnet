using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

// sample schema that would be auto generated in AOT scenarios
public partial class SampleAotSchema : AotSchema
{
    protected override void Configure(IServiceProvider services)
    {
        AddAotType<AutoOutputGraphType_StarWarsQuery>();

        this.RegisterTypeMapping<IStarWarsCharacter, AutoOutputGraphType_IStarWarsCharacter>();
        AddAotType<AutoOutputGraphType_IStarWarsCharacter>();
        this.RegisterTypeMapping<Episodes, EnumerationGraphType<Episodes>>(); // uses AOT-safe reflection during initialization only
        AddAotType<EnumerationGraphType<Episodes>>();
        this.RegisterTypeMapping<Droid, AutoOutputGraphType_Droid>();
        AddAotType<AutoOutputGraphType_Droid>();
        this.RegisterTypeMapping<Human, AutoOutputGraphType_Human>();
        AddAotType<AutoOutputGraphType_Human>();
        this.RegisterTypeMapping<StarWarsMutation, AutoOutputGraphType_StarWarsMutation>();
        AddAotType<AutoOutputGraphType_StarWarsMutation>();
        this.RegisterTypeMapping<HumanInput, AutoInputGraphType_HumanInput>();
        AddAotType<AutoInputGraphType_HumanInput>();

        // register relay types
        this.RegisterTypeMapping<PageInfo, AutoOutputGraphType_PageInfo>();
        AddAotType<AutoOutputGraphType_PageInfo>();
        this.RegisterTypeMapping<Edge<IStarWarsCharacter>, AutoOutputGraphType_Edge_IStarWarsCharacter>();
        AddAotType<AutoOutputGraphType_Edge_IStarWarsCharacter>();
        this.RegisterTypeMapping<Connection<IStarWarsCharacter>, AutoOutputGraphType_Connection_IStarWarsCharacter>();
        AddAotType<AutoOutputGraphType_Connection_IStarWarsCharacter>();

        // register constructors for built-in types
        AddAotType<IntGraphType>();
        AddAotType<StringGraphType>();
        AddAotType<BooleanGraphType>();
        AddAotType<FloatGraphType>();
        AddAotType<IdGraphType>();

        // register list conversions (none needed in this sample)

        // [AutoQueryType] is same as [AutoOutputType] plus it configures the Query root type
        Query = this.GetRequiredService<AutoOutputGraphType_StarWarsQuery>();
    }

    protected override SchemaTypesBase CreateSchemaTypes()
    {
        // add custom mapping provider that knows about generated types or type mappings registered via [HasType] attribute
        // does not add mapping providers for non-standard built-in types or auto-registering types
        // however, mappings required by introspection and GraphQL specification are still included
        var graphTypeMappingProviders = this.GetService<IEnumerable<IGraphTypeMappingProvider>>();
        var mappingProviders = graphTypeMappingProviders != null
            ? graphTypeMappingProviders.Prepend(new GeneratedMappingProvider())
            : [new GeneratedMappingProvider()];
        return new SchemaTypes(this, this, mappingProviders, OnBeforeInitializeType);
    }

    private class GeneratedMappingProvider : IGraphTypeMappingProvider
    {
        public Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredGraphType)
        {
            if (preferredGraphType != null)
                return preferredGraphType;

            // type dictated by attribute on SampleAotSchema class
            if (clrType == typeof(StarWarsQuery) && !isInputType)
            {
                return typeof(AutoOutputGraphType_StarWarsQuery);
            }

            // type required by introspection
            if (clrType == typeof(int))
                return typeof(IntGraphType);
            if (clrType == typeof(string))
                return typeof(StringGraphType);
            if (clrType == typeof(bool))
                return typeof(BooleanGraphType);

            // defined by GraphQL specification, so make that available too
            if (clrType == typeof(double))
                return typeof(FloatGraphType);

            // any other types (including built-in ones) can be defined by the developer via [HasType]

            return null;
        }
    }
}
