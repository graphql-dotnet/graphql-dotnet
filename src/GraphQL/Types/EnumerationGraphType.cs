using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;

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
            AddValue(new EnumValueDefinition
            {
                Name = name,
                Description = description,
                Value = value,
                DeprecationReason = deprecationReason
            });
        }

        public void AddValue(EnumValueDefinition value)
        {
            Values.Add(value);
        }

        public EnumValues Values { get; private set; }

        public override object ParseValue(object value)
        {
            var found = Values.FirstOrDefault(v => v.Value.Equals(value));
            return found != null ? found.Name : null;
        }

        public override object ParseLiteral(IValue value)
        {
            var enumVal = value as EnumValue;
            return ParseValue(enumVal?.Name);
        }

        public object GetValue(string name)
        {
            var found = Values.FirstOrDefault(v =>
                StringComparer.InvariantCultureIgnoreCase.Equals(v.Name, name));
            return found != null ? found.Value : null;
        }
    }

    public class EnumValues : IEnumerable<EnumValueDefinition>
    {
        private readonly List<EnumValueDefinition> _values = new List<EnumValueDefinition>();

        public void Add(EnumValueDefinition value)
        {
            _values.Add(value);
        }

        public IEnumerator<EnumValueDefinition> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class EnumValueDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object Value { get; set; }
    }
}
