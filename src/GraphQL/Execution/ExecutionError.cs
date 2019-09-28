using GraphQL.Language.AST;
using GraphQLParser;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GraphQL
{
    [Serializable]
    public class ExecutionError : Exception
    {
        private static readonly ConcurrentDictionary<Type, string> _exceptionErrorCodes = new ConcurrentDictionary<Type, string>();

        private List<ErrorLocation> _errorLocations;

        public ExecutionError(string message)
            : base(message)
        {
        }

        public ExecutionError(string message, IDictionary data)
            : base(message)
        {
            SetData(data);
        }

        public ExecutionError(string message, Exception exception)
            : base(message, exception)
        {
            SetCode(exception);
            SetData(exception);
        }

        public IEnumerable<ErrorLocation> Locations => _errorLocations;

        internal bool HasLocations => _errorLocations != null && _errorLocations.Count > 0;

        public string Code { get; set; }

        internal bool HasCodes => InnerException != null || !string.IsNullOrWhiteSpace(Code);

        internal IEnumerable<string> Codes
        {
            get
            {
                // Code could be set explicitly, and not through the constructor with the exception
                if (!string.IsNullOrWhiteSpace(Code) && (InnerException == null || Code != GetErrorCode(InnerException)))
                    yield return Code;

                var current = InnerException;

                while (current != null)
                {
                    yield return GetErrorCode(current);
                    current = current.InnerException;
                }
            }
        }

        public IEnumerable<string> Path { get; set; }

        public Dictionary<string, object> DataAsDictionary { get; } = new Dictionary<string, object>();

        public override IDictionary Data => DataAsDictionary;

        public void AddLocation(int line, int column)
        {
            if (_errorLocations == null)
            {
                _errorLocations = new List<ErrorLocation>();
            }

            _errorLocations.Add(new ErrorLocation { Line = line, Column = column });
        }

        private void SetCode(Exception exception)
        {
            if (exception != null)
                Code = GetErrorCode(exception);
        }

        private void SetData(Exception exception)
        {
            if (exception?.Data != null)
                SetData(exception.Data);
        }

        private void SetData(IDictionary dict)
        {
            if (dict != null)
            {
                foreach (DictionaryEntry keyValuePair in dict)
                {
                    var key = keyValuePair.Key.ToString();
                    var value = keyValuePair.Value;
                    Data[key] = value;
                }
            }
        }

        private static string GetErrorCode(Exception exception) => _exceptionErrorCodes.GetOrAdd(exception.GetType(), NormalizeErrorCode);

        private static string NormalizeErrorCode(Type exceptionType)
        {
            var code = exceptionType.Name;

            if (code.EndsWith(nameof(Exception)))
            {
                code = code.Substring(0, code.Length - nameof(Exception).Length);
            }

            if (code.StartsWith("GraphQL"))
            {
                code = code.Substring("GraphQL".Length);
            }

            return GetAllCapsRepresentation(code);
        }

        private static string GetAllCapsRepresentation(string str)
        {
            return Regex
                .Replace(NormalizeString(str), @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
                .ToUpperInvariant();
        }

        private static string NormalizeString(string str)
        {
            str = str?.Trim();
            return string.IsNullOrWhiteSpace(str)
                ? string.Empty
                : NormalizeTypeName(str);
        }

        private static string NormalizeTypeName(string name)
        {
            var tickIndex = name.IndexOf('`');
            return tickIndex >= 0
                ? name.Substring(0, tickIndex)
                : name;
        }
    }

    public struct ErrorLocation
    {
        public int Line { get; set; }

        public int Column { get; set; }
    }

    public static class ExecutionErrorExtensions
    {
        public static void AddLocation(this ExecutionError error, AbstractNode abstractNode, Document document)
        {
            if (abstractNode == null)
                return;

            if (document != null)
            {
                var location = new Location(new Source(document.OriginalQuery), abstractNode.SourceLocation.Start);
                error.AddLocation(location.Line, location.Column);
            }
            else if (abstractNode.SourceLocation.Line > 0 && abstractNode.SourceLocation.Column > 0)
            {
                error.AddLocation(abstractNode.SourceLocation.Line, abstractNode.SourceLocation.Column);
            }
        }
    }
}
