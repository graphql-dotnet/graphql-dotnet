namespace GraphQL.Utilities.Federation.Enums
{
    /// <summary>
    /// This controls the Federation Version used to build the schema
    /// For version 2.x, this changes the value in the @link directive
    /// </summary>
    enum FederationVersion
    {
        V1 = 1,
        V20 = 21
        // V2_1 = 2.1

        // "2.0",
        // "2.1",
        // "2.2",
        // "2.3",
        // "2.4",
        // "2.5"
    }
}
