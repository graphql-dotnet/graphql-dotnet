using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language
{
    public class Arguments : IEnumerable<Argument>
    {
        private readonly List<Argument> _arguments = new List<Argument>();

        public void Add(Argument arg)
        {
            _arguments.Add(arg);
        }

        public IValue ValueFor(string name)
        {
            var arg = _arguments.FirstOrDefault(x => x.Name == name);
            return arg != null ? arg.Value : null;
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
