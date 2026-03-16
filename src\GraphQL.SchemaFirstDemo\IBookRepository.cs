using GraphQL.SchemaFirstDemo.Models;

namespace GraphQL.SchemaFirstDemo;

/// <summary>Abstraction over the in-memory book store.</summary>
public interface IBookRepository
{
    IEnumerable<Book> GetAll();
    Book? GetById(string id);
    Book Add(AddBookInput input);
    bool Delete(string id);
}
