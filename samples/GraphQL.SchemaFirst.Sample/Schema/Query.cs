namespace GraphQL.SchemaFirst.Sample.Schema;

public class Query
{
    public Droid? Hero([FromServices] DroidRepository repository)
        => repository.GetHero();

    public IEnumerable<Droid> Droids([FromServices] DroidRepository repository)
        => repository.GetAllDroids();

    public Droid? Droid([FromServices] DroidRepository repository, string id)
        => repository.GetDroidById(id);
}
