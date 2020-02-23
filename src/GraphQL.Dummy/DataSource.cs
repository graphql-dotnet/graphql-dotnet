using System.Threading.Tasks;

namespace GraphQL.Dummy
{
    public static class DataSource
    {
        public static Task GetSomething() => Task.FromResult(new InternalClass());
    }
}
