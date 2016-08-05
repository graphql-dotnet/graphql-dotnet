using System.Text.RegularExpressions;

namespace GraphQL.Language
{
    public class Location
    {
        public Location(ISource source, int position)
        {
            var lineRegex = new Regex("\r\n|[\n\r]", RegexOptions.ECMAScript | RegexOptions.Compiled);
            this.Line = 1;
            this.Column = position + 1;

            var matches = lineRegex.Matches(source.Body);
            foreach (Match match in matches)
            {
                if (match.Index >= position)
                    break;

                this.Line++;
                this.Column = position + 1 - (match.Index + matches[0].Length);
            }
        }

        public int Column { get; private set; }
        public int Line { get; private set; }
    }
}
