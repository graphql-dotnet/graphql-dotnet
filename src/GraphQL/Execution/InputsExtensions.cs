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
            => dictionary == null ? Inputs.Empty : new Inputs(dictionary);
    }
}
