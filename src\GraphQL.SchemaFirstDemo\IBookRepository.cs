using GraphQL.SchemaFirstDemo.Models;

namespace GraphQL.SchemaFirstDemo;

public interface IBookRepository
{
    IEnumerable<Book> GetAll();
    Book? GetById(string id);
    Book Add(AddBookInput input);
    bool Delete(string id);
}
