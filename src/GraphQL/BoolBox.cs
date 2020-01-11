namespace GraphQL
{
    internal static class BoolBox
    {
        public static readonly object True = true;
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
        public static object Boxed(this bool? value) => value switch
        {
            true => True,
            false => False,
            null => null
        };
    }
}
