using System.Reflection;
using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;
using GraphQLParser.AST;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoInputGraphType_HumanInput : AotAutoRegisteringInputObjectGraphType<HumanInput>
    {
        private readonly MemberInfo[] _members;
        private readonly FieldType[] _fields;

        public AutoInputGraphType_HumanInput()
        {
            _members = [
                typeof(HumanInput).GetProperty(nameof(HumanInput.Name))!,
                typeof(HumanInput).GetProperty(nameof(HumanInput.HomePlanet))!,
            ];
            _fields = ProvideFields().ToArray();
            foreach (var fieldType in _fields)
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members;

        public override object ParseDictionary(IDictionary<string, object?> value, IValueConverter valueConverter)
        {
            // supports arguments in constructors, required and init
            // exactly same semantics as non-aot input object types
            return new HumanInput
            {
                Name = ParseField<string>(_fields[0]),
                HomePlanet = ParseField<string?>(_fields[1]),
            };

            TFieldType ParseField<TFieldType>(FieldType fieldType)
            {
                if (value.TryGetValue(fieldType.Name, out var fieldValue))
                {
                    return (TFieldType)valueConverter.GetPropertyValue(fieldValue, typeof(string), fieldType.ResolvedType!)!;
                }
                return default!;
            }
        }

        public override bool IsValidDefault(object value)
        {
            if (value is not HumanInput obj)
                return false;

            if (_fields[0].ResolvedType!.IsValidDefault(obj.Name))
                return false;
            if (_fields[1].ResolvedType!.IsValidDefault(obj.HomePlanet))
                return false;

            return true;
        }

        public override GraphQLValue ToAST(object? value)
        {
            if (value == null)
                return new GraphQLNullValue();

            if (value is not HumanInput obj)
                return null!; //allowed return to indicate failure

            var objectValue = new GraphQLObjectValue
            {
                Fields = new(_fields.Length)
            };

            ProcessField(_fields[0], obj.Name);
            ProcessField(_fields[1], obj.HomePlanet);

            return objectValue;

            void ProcessField(FieldType fieldType, object? value)
            {
                var ast = fieldType.ResolvedType!.ToAST(value)
                    ?? throw new InvalidOperationException("Could not convert value in HumanInput.Name to AST");
                if (ast is not GraphQLNullValue || fieldType.DefaultValue != null)
                    objectValue.Fields.Add(new(new(fieldType.Name), ast));
            }
        }
    }
}
