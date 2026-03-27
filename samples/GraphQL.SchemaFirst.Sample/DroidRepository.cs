using GraphQL.SchemaFirst.Sample.Schema;

namespace GraphQL.SchemaFirst.Sample;

public class DroidRepository
{
    private readonly List<Droid> _droids =
    [
        new Droid { Id = "1", Name = "R2-D2", PrimaryFunction = "Astromech" },
        new Droid { Id = "2", Name = "C-3PO", PrimaryFunction = "Protocol" },
    ];

    public Droid? GetHero() => _droids.FirstOrDefault();

    public IEnumerable<Droid> GetAllDroids() => _droids;

    public Droid? GetDroidById(string id) => _droids.SingleOrDefault(d => d.Id == id);
}
