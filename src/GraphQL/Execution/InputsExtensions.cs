using System.Collections.Generic;

namespace GraphQL
{
    public static class InputsExtensions
    {
        /// <summary>
        /// Converts a dictionary into an <see cref="Inputs"/>.
        /// </summary>
        /// <param name="json">A dictionary.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this Dictionary<string, object> dictionary)
            => dictionary?.Count > 0 ? new Inputs(dictionary) : Inputs.Empty;
    }
}
