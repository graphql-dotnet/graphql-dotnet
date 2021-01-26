using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GraphQL.Benchmarks.Merge
{
    internal sealed class BenchmarkResult
    {
        public static BenchmarkResult Parse(string file)
        {
            var result = new BenchmarkResult();

            string[] lines = File.ReadAllLines(file);
            int i = 0;
            for (; i < lines.Length; ++i)
            {
                result.Header.Add(lines[i]);
                if (lines[i] == "```")
                    break;
            }

            ++i;

            result.Columns = Columns.Parse(lines, i);
            return result;
        }

        public List<string> Header = new List<string>();

        public Columns Columns { get; set; } = new Columns();

        public BenchmarkResult RemoveColumns(params string[] names)
        {
            foreach (string name in names)
                Columns.Remove(name);
            return this;
        }

        public void Save(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            var sb = new StringBuilder(2048);
            foreach (string line in Header)
                sb.AppendLine(line);

            sb.Append('|');
            foreach (string name in Columns.Names)
                sb.Append(name).Append('|');

            sb.AppendLine().Append('|');
            for (int i = 0; i < Columns.Names.Count; ++i)
                sb.Append("-----").Append('|');
            sb.AppendLine();

            foreach (var line in Columns.Data)
            {
                sb.Append('|');
                foreach (string item in line)
                    sb.Append(item).Append('|');
                sb.AppendLine();
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
