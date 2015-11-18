using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class EnumerationGraphType : ScalarGraphType
    {
        public EnumerationGraphType()
        {
            Values = new EnumValues();
        }

        public void AddValue(string name, string description, object value)
        {
            AddValue(new EnumValue
            {
                Name = name,
                Description = description,
                Value = value
            });
        }

        public void AddValue(EnumValue value)
        {
            Values.Add(value);
        }

        public EnumValues Values { get; private set; }

        public override object Coerce(object value)
        {
            var found = Values.FirstOrDefault(v => v.Value.Equals(value));
            return found != null ? found.Name : null;
        }

        public object GetValue(string name)
        {
            var found = Values.FirstOrDefault(v =>
                StringComparer.InvariantCultureIgnoreCase.Equals(v.Name, name));
            return found != null ? found.Value : null;
        }
    }

    public class EnumValues : IEnumerable<EnumValue>
    {
        private readonly List<EnumValue> _values = new List<EnumValue>();

        public void Add(EnumValue value)
        {
            _values.Add(value);
        }

        public IEnumerator<EnumValue> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class EnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object Value { get; set; }
    }
}
