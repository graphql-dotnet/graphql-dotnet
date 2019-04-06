using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GraphQL.Language.AST;
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
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            NameValidator.ValidateName(value.Name, "enum");
            Values.Add(value);
        }

        public EnumValues Values { get; }

        public override object Serialize(object value)
        {
            var valueString = value.ToString();
            var foundByName = Values.FirstOrDefault(v => v.Name.Equals(valueString, StringComparison.OrdinalIgnoreCase));
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
            return !(value is EnumValue enumValue) ? null : ParseValue(enumValue.Name);
        }
    }

    public class EnumerationGraphType<TEnum> : EnumerationGraphType
    {
        public EnumerationGraphType()
        {
            var type = typeof(TEnum);
            var names = Enum.GetNames(type);
            var enumMembers = names.Select(n => (name: n, member: type
                    .GetMember(n, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .First()));
            var enumGraphData = enumMembers.Select(e => (
                name: ChangeEnumCase(e.name),
                value: Enum.Parse(type, e.name),
                description: (e.member.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description
            ));

            Name = Name ?? StringUtils.ToPascalCase(type.Name);

            foreach (var (name, value, description) in enumGraphData)
            {
                AddValue(name, description, value);
            }
        }

        protected virtual string ChangeEnumCase(string val)
        {
            return StringUtils.ToConstantCase(val);
        }
    }

    public class EnumValues : IEnumerable<EnumValueDefinition>
    {
        private readonly List<EnumValueDefinition> _values = new List<EnumValueDefinition>();

        public void Add(EnumValueDefinition value)
        {
            _values.Add(value ?? throw new ArgumentNullException(nameof(value)));
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
