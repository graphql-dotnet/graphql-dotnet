using System.Collections.Generic;

namespace GraphQL.Tests.Validation
{
    public class ValidationErrorAssertion
    {
        private readonly List<ErrorLocation> _locations = new List<ErrorLocation>();

        public string Message { get; set; }
        public IEnumerable<ErrorLocation> Locations => _locations;

        public void Loc(int line, int column)
        {
            _locations.Add(new ErrorLocation {Line = line, Column = column});
        }
    }
}
