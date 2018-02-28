namespace GraphQL.DataLoader
{
    public interface IDataLoaderContextAccessor
    {
        DataLoaderContext Context { get; set; }
    }
}
