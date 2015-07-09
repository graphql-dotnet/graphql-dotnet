using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Arguments : IEnumerable<Argument>
    {
        private readonly List<Argument> _arguments = new List<Argument>();

        public void Add(Argument arg)
        {
            _arguments.Add(arg);
        }

        public IEnumerator<Argument> GetEnumerator()
        {
            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}