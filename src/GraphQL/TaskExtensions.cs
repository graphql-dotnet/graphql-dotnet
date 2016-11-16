using System.Threading.Tasks;

namespace GraphQL
{
    public static class TaskExtensions
    {
        public static Task CompletedTask
        {
            get
            {
                object result = null;
                return Task.FromResult(result);
            }
        }
    }
}
