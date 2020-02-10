using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GraphQL
{
    public class Inputs : ReadOnlyDictionary<string, object>
    {
        public static readonly Inputs Empty = new Inputs(new Dictionary<string, object>());

        public Inputs(IDictionary<string, object> dictionary)
            : base(dictionary)
        {
        }
    }
}
