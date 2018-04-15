using System.Collections.Generic;

namespace GraphQL.Utilities
{
    public class SchemaPrinterOptions
    {
        public List<string> CustomScalars { get; set; } = new List<string>();

        public bool IncludeDescriptions { get; set; } = false;

        public bool IncludeDeprecationReasons { get; set; } = false;
    }
}