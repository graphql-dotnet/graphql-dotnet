using GraphQLParser;

namespace GraphQL.Tests.Validation;

public class ValidationErrorAssertion
{
    private readonly List<Location> _locations = new();

    public string Message { get; set; }
    public IList<Location> Locations => _locations;

    public void Loc(int line, int column)
    {
        _locations.Add(new Location(line, column));
    }
}
