using System;
using System.Linq;

namespace GraphQL.Language
{
    public class GraphQLSyntaxErrorException : GraphQLException
    {
        public GraphQLSyntaxErrorException(string description, ISource source, int location)
            : base(ComposeMessage(description, source, location))
        {
        }

        private static string ComposeMessage(string description, ISource source, int loc)
        {
            var location = new Location(source, loc);

            return $"Syntax Error GraphQL ({location.Line}:{location.Column}) {description}" +
                "\n" + HighlightSourceAtLocation(source, location);
        }

        private static string HighlightSourceAtLocation(ISource source, Location location)
        {
            var line = location.Line;
            var prevLineNum = (line - 1).ToString();
            var lineNum = line.ToString();
            var nextLineNum = (line + 1).ToString();
            var padLen = nextLineNum.Length;
            var lines = source.Body
                .Split(new [] { "\n" }, StringSplitOptions.None)
                .Select(ReplaceWithUnicodeRepresentation)
                .ToArray();

            return
                (line >= 2 ? LeftPad(padLen, prevLineNum) + ": " + lines[line - 2] + "\n" : string.Empty) +
                LeftPad(padLen, lineNum) + ": " + lines[line - 1] + "\n" +
                LeftPad(1 + padLen + location.Column, string.Empty) + "^" + "\n" +
                (line < lines.Length ? LeftPad(padLen, nextLineNum) + ": " + lines[line] + "\n" : string.Empty);
        }

        private static string LeftPad(int length, string str)
        {
            string pad = string.Empty;

            for (var i = 0; i < length - str.Length; i++)
                pad += " ";

            return pad + str;
        }

        private static string ReplaceWithUnicodeRepresentation(string str)
        {
            foreach (var code in str.ToList())
            {
                if (code < 0x0020 && code != 0x0009 && code != 0x000A && code != 0x000D)
                    str = str.Replace(string.Empty + code, "\\u" + ((int)code).ToString("D4"));
            }

            return str;
        }
    }
}
