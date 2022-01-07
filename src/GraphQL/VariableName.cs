namespace GraphQL
{
    public struct VariableName
    {
        public VariableName(VariableName variableName, int index)
        {
            Name = variableName;
            Index = index;
            ChildName = null;
        }

        public VariableName(VariableName variableName, string childName)
        {
            if (variableName.ChildName == null)
            {
                Name = variableName.Name;
                Index = variableName.Index;
            }
            else
            {
                Name = variableName;
                Index = default;
            }
            ChildName = childName;
        }

        public string Name { get; set; }

        public int? Index { get; set; }

        public string? ChildName { get; set; }

        public override string ToString() => (!Index.HasValue ? Name : Name + "[" + Index.Value + "]") + (ChildName != null ? '.' + ChildName : null);

        public static implicit operator VariableName(string name) => new VariableName { Name = name };

        public static implicit operator string(VariableName variableName) => variableName.ToString();
    }
}
