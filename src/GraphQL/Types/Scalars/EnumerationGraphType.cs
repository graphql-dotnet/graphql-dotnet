using GraphQL.Language.AST;
using GraphQL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            var foundByName = Values.FindByName(valueString);
            if (foundByName != null)
            {
                return foundByName.Name;
            }

            var foundByValue = Values.FindByValue(value);
            return foundByValue?.Name;
        }

        public override object ParseValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var found = Values.FindByName(value.ToString());
            return found?.Value;
        }

        public override object ParseLiteral(IValue value)
        {
            return !(value is EnumValue enumValue) ? null : ParseValue(enumValue.Name);
        }
    }

    /// <summary>
    /// Allows you to automatically register the necessary enumeration members for the specified enum.
    /// Supports <see cref="DescriptionAttribute"/> and <see cref="ObsoleteAttribute"/>.
    /// Also it can get descriptions for enum fields from the xml comments.
    /// </summary>
    /// <typeparam name="TEnum"> The enum to take values from. </typeparam>
    public class EnumerationGraphType<TEnum> : EnumerationGraphType where TEnum : Enum
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
                description: e.member.Description(),
                deprecation: e.member.ObsoleteMessage()
            ));

            Name = StringUtils.ToPascalCase(type.Name);
            Description ??= typeof(TEnum).Description();
            DeprecationReason ??= typeof(TEnum).ObsoleteMessage();

            foreach (var (name, value, description, deprecation) in enumGraphData)
            {
                AddValue(name, description, value, deprecation);
            }
        }

        protected virtual string ChangeEnumCase(string val) => StringUtils.ToConstantCase(val);
    }

    public class EnumValues : IEnumerable<EnumValueDefinition>
    {
        private readonly List<EnumValueDefinition> _values = new List<EnumValueDefinition>();

        public EnumValueDefinition this[string name] => FindByName(name);

        public void Add(EnumValueDefinition value) => _values.Add(value ?? throw new ArgumentNullException(nameof(value)));

        public EnumValueDefinition FindByName(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in _values)
                if (def.Name.Equals(name, comparison))
                    return def;

            return null;
        }

        public EnumValueDefinition FindByValue(object value)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in _values)
                if (def.Value.Equals(value))
                    return def;

            return null;
        }

        public IEnumerator<EnumValueDefinition> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class EnumValueDefinition : MetadataProvider
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object Value { get; set; }
    }
}
