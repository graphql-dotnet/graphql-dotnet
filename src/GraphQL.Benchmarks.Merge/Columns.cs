using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Benchmarks.Merge
{
    internal sealed class Columns
    {
        public static Columns Parse(string[] content, int from)
        {
            var columns = new Columns
            {
                Names = content[from].Split('|').Select(i => i.Trim()).ToList()
            };
            columns.Names.RemoveAt(0);
            columns.Names.RemoveAt(columns.Names.Count - 1);

            from += 2; // skip |--------|--------|

            for (; from < content.Length; ++from)
            {
                var data = content[from].Split('|').Select(i => i.Trim()).ToList();
                data.RemoveAt(0);
                data.RemoveAt(data.Count - 1);
                columns.Data.Add(data);
            }

            return columns;
        }

        public List<string> Names { get; set; } = new List<string>();

        public List<List<string>> Data { get; set; } = new List<List<string>>();

        public void Remove(string columnName)
        {
            int index = Names.IndexOf(columnName);
            if (index == -1)
                return;

            Names.Remove(columnName);

            foreach (var line in Data)
                line.RemoveAt(index);
        }
    }
}
