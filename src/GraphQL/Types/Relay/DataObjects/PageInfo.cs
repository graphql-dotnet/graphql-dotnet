namespace GraphQL.Types.Relay.DataObjects
{
    /// <summary>
    /// Contains pagination information relating to the result data set.
    /// </summary>
    public class PageInfo
    {
        /// <summary>
        /// Indicates if there are additional pages of data that can be returned.
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Indicates if there are prior pages of data that can be returned.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// The cursor of the first node in the result data set.
        /// </summary>
        public string? StartCursor { get; set; }

        /// <summary>
        /// The cursor of the last node in the result data set.
        /// </summary>
        public string? EndCursor { get; set; }
    }
}
