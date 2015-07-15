using System.Collections.Generic;

namespace GraphQL.Types
{
    public class ObjectGraphType : GraphType, IImplementInterfaces
    {
        private readonly List<InterfaceGraphType> _interfaces = new List<InterfaceGraphType>();

        public IEnumerable<InterfaceGraphType> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public void Interface<TInterface>()
            where TInterface : InterfaceGraphType, new()
        {
            _interfaces.Add(new TInterface());
        }
    }
}
