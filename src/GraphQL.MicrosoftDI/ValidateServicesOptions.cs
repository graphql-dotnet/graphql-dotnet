namespace GraphQL.MicrosoftDI;

/// <summary>
/// Options for <see cref="ValidateServicesSchemaConfigurator"/>
/// </summary>
internal class ValidateServicesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the schema validator is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
