namespace GraphQL
{
    public static class StringExtensions
    {
        public static string ToFormat(this string format, params object[] args)
        {
            return string.Format(format, args);
        }
    }
}
