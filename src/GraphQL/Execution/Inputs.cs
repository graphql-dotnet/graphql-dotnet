using System.Collections.Generic;

namespace GraphQL
{
    public class Inputs : Dictionary<string, object>
    {
        public Inputs()
        {
        }

        public Inputs(IDictionary<string, object> dictionary)
            : base(dictionary)
        {
        }
    }
}
