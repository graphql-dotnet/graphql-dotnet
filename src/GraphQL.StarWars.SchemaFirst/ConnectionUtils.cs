using GraphQL.StarWars.SchemaFirst.Models;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.SchemaFirst;

public static class ConnectionUtils
{
    public static Connection<IStarWarsCharacter> ToConnection(
        List<IStarWarsCharacter> items,
        IResolveFieldContext context,
        int? first,
        string? after,
        int? last,
        string? before)
    {
        var edges = items.Select(item => new Edge<IStarWarsCharacter>
        {
            Cursor = item.Cursor ?? string.Empty,
            Node = item
        }).ToList();

        // Apply pagination
        var startIndex = 0;
        var endIndex = edges.Count;

        if (!string.IsNullOrEmpty(after))
        {
            var afterIndex = edges.FindIndex(e => e.Cursor == after);
            if (afterIndex >= 0)
                startIndex = afterIndex + 1;
        }

        if (!string.IsNullOrEmpty(before))
        {
            var beforeIndex = edges.FindIndex(e => e.Cursor == before);
            if (beforeIndex >= 0)
                endIndex = beforeIndex;
        }

        if (first.HasValue)
        {
            endIndex = Math.Min(startIndex + first.Value, endIndex);
        }

        if (last.HasValue)
        {
            startIndex = Math.Max(endIndex - last.Value, startIndex);
        }

        var paginatedEdges = edges.Skip(startIndex).Take(endIndex - startIndex).ToList();

        return new Connection<IStarWarsCharacter>
        {
            TotalCount = items.Count,
            Edges = paginatedEdges,
            PageInfo = new PageInfo
            {
                HasNextPage = endIndex < edges.Count,
                HasPreviousPage = startIndex > 0,
                StartCursor = paginatedEdges.FirstOrDefault()?.Cursor,
                EndCursor = paginatedEdges.LastOrDefault()?.Cursor
            }
        };
    }
}
