using GraphQL.Language;

namespace GraphQL.Execution
{
    public interface IDocumentBuilder
    {
        Document Build(string data);
    }
}
