using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQLParser.AST;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoInputGraphType_HumanInput : ComplexGraphType<HumanInput>, IInputObjectGraphType
    {
        private readonly FieldType[] _inputFields;

        /// <inheritdoc/>
        public bool IsOneOf { get; set; }

        public AutoInputGraphType_HumanInput()
        {
            // 1. set default name from type name
            Name = "HumanInput";

            // 2. apply graph type attributes (this happens before fields are added)

            // 3. add fields
            _inputFields = [
                AddField_Name(),
                AddField_HomePlanet(),
            ];
        }

        private FieldType AddField_Name()
        {
            var fieldType = new FieldType
            {
                Name = "Name",
                Type = typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>),
            };

            // process attributes on property (none in this case)

            AddField(fieldType);
            return fieldType;
        }

        private FieldType AddField_HomePlanet()
        {
            var fieldType = new FieldType
            {
                Name = "HomePlanet",
                Type = typeof(GraphQLClrInputTypeReference<string>),
            };

            // process attributes on property (none in this case)

            AddField(fieldType);
            return fieldType;
        }

        public object ParseDictionary(IDictionary<string, object?> value, IValueConverter valueConverter)
        {
            // supports arguments in constructors, required and init
            // exactly same semantics as non-aot input object types
            return new HumanInput
            {
                Name = ParseField<string>(_inputFields[0]),
                HomePlanet = ParseField<string?>(_inputFields[1]),
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

        public bool IsValidDefault(object value)
        {
            if (value is not HumanInput obj)
                return false;

            if (_inputFields[0].ResolvedType!.IsValidDefault(obj.Name))
                return false;
            if (_inputFields[1].ResolvedType!.IsValidDefault(obj.HomePlanet))
                return false;

            return true;
        }

        public GraphQLValue ToAST(object? value)
        {
            if (value == null)
                return new GraphQLNullValue();

            if (value is not HumanInput obj)
                return null!; //allowed return to indicate failure

            var objectValue = new GraphQLObjectValue
            {
                Fields = new(_inputFields.Length)
            };

            // Handle 'name'
            ProcessField(_inputFields[0], obj.Name);
            ProcessField(_inputFields[1], obj.HomePlanet);

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
