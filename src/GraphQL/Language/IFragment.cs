namespace GraphQL.Language
{
    public interface IFragment
    {
    }

    public interface IHaveFragmentType : IFragment
    {
        string Type { get; set; }
    }
}
