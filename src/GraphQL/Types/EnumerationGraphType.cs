using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using System.Reflection;
using GraphQL.Utilities;

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

        public EnumValues Values { get; }

        public override object Serialize(object value)
        {
            var foundByName = Values.FirstOrDefault(v => v.Name.Equals(value + "", StringComparison.OrdinalIgnoreCase));
            if (foundByName != null)
            {
                return foundByName.Name;
            }

            var found = Values.FirstOrDefault(v => v.Value.Equals(value));
            return found?.Name;
        }

        public override object ParseValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var found = Values.FirstOrDefault(v =>
                StringComparer.OrdinalIgnoreCase.Equals(v.Name, value.ToString()));
            return found?.Value;
        }

        public override object ParseLiteral(IValue value)
        {
            var enumValue = value as EnumValue;
            return enumValue == null ? null : ParseValue(enumValue.Name);
        }
    }

    public class EnumerationGraphType<TEnum> : EnumerationGraphType
    {
        public EnumerationGraphType()
        {
            var type = typeof(TEnum);
            var typeInfo = type.GetTypeInfo();

            Name = Name ?? StringUtils.ToPascalCase(type.Name);

            foreach (var enumName in Enum.GetNames(type))
            {
                var enumMember = type
                    .GetMember(enumName, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .First();

                AddValue(StringUtils.ToConstantCase(enumMember.Name), null, Enum.Parse(type, enumName));
            }
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
