namespace GraphQL.SchemaFirstDemo.Models;

/// <summary>
/// Book genre.  Enum member names are matched case-insensitively to the SDL
/// <c>Genre</c> enum values (<c>FICTION</c>, <c>NON_FICTION</c>, …).
/// </summary>
public enum Genre
{
    Fiction,
    NonFiction,
    Science,
    History,
    Biography,
}
