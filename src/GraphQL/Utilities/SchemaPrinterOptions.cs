namespace GraphQL.Utilities
{
    public class SchemaPrinterOptions
    {
        /// <summary>
        /// If enabled, then all types defined in the schema will be printed, else only really used.
        /// </summary>
        /// <remarks>https://github.com/graphql/graphql-spec/issues/648</remarks>
        public bool IncludeAllTypes { get; set; }

        public bool IncludeDescriptions { get; set; }

        public bool IncludeDeprecationReasons { get; set; }

        public bool OldImplementsSyntax { get; set; }
    }
}
