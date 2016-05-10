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

        public void AddValue(string name, string description, object value, string deprecationReason = null)
        {
            AddValue(new EnumValue
            {
                Name = name,
                Description = description,
                Value = value,
                DeprecationReason = deprecationReason
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
            if (found != null) return found.Name;
            return Values.FirstOrDefault(v => v.Name.Equals(value))?.Value;
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
