namespace GraphQL
{
    /// <summary> Boolean values to avoid boxing. </summary>
    public static class BoolBox
    {
        /// <summary>
        /// Shared field for 'true' value.
        /// </summary>
        public static readonly object True = true;

        /// <summary>
        /// Shared field for 'false' value.
        /// </summary>
        public static readonly object False = false;

        /// <summary> This method avoids boxing boolean values. </summary>
        /// <param name="value"> Original boolean value. </param>
        /// <returns> Shared static boxed boolean value. </returns>
        public static object Boxed(this bool value) => value switch
        {
            true => True,
            false => False,
        };

        /// <summary> This method avoids boxing boolean values. </summary>
        /// <param name="value"> Original boolean value. </param>
        /// <returns> Shared static boxed boolean value or <c>null</c>. </returns>
        public static object? Boxed(this bool? value) => value switch
        {
            true => True,
            false => False,
            null => null
        };
    }
}
