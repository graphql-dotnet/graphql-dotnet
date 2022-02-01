namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for converting a dictionary into <see cref="Inputs"/>.
    /// </summary>
    public static class InputsExtensions
    {
        /// <summary>
        /// Converts a dictionary into an <see cref="Inputs"/>.
        /// </summary>
        /// <param name="dictionary">A dictionary.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this Dictionary<string, object?> dictionary)
            => dictionary?.Count > 0 ? new Inputs(dictionary) : Inputs.Empty;
    }
}
