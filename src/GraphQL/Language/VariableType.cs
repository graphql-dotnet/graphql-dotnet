namespace GraphQL.Language
{
    public class VariableType
    {
        public VariableType()
        {
            AllowsNull = true;
        }

        public string Name { get; set; }

        public bool IsList { get; set; }

        public bool AllowsNull { get; set; }

        public string FullName
        {
            get
            {
                var typeNameFormat = IsList ? "[{0}]{1}" : "{0}{1}";
                return string.Format(typeNameFormat, Name, AllowsNull ? "" : "!");
            }
        }
    }
}
