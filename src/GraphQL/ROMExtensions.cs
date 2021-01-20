using System.Collections.Generic;
using GraphQLParser;

namespace GraphQL
{
    public static class ROMExtensions
    {
        public static bool Contains(this List<string> list, ROM rom)
        {
            foreach(var l in list)
            {
                if (l == rom)
                    return true;
            }
            return false;
        }
    }
}
