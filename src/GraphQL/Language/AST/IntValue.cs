using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class IntValue : AbstractNode, IValue
    {
        public IntValue(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public override string ToString()
        {
            return "IntValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(IntValue other)
        {
            return Value == other.Value;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IntValue) obj);
        }
    }

    public class LongValue : AbstractNode, IValue
    {
        public LongValue(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override string ToString()
        {
            return "LongValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(LongValue other)
        {
            return Value == other.Value;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LongValue)obj);
        }
    }

    public class DecimalValue : AbstractNode, IValue
    {
        public DecimalValue(decimal value)
        {
            Value = value;
        }

        public decimal Value { get; }

        public override string ToString()
        {
            return "DecimalValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(DecimalValue other)
        {
            return Value == other.Value;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecimalValue)obj);
        }
    }

    public class FloatValue : AbstractNode, IValue
    {
        public FloatValue(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public override string ToString()
        {
            return "FloatValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(FloatValue other)
        {
            return Value == other.Value;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((FloatValue)obj);
        }
    }

    public class StringValue : AbstractNode, IValue
    {
        public StringValue(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return "StringValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(StringValue other)
        {
            return string.Equals(Value, other.Value);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StringValue)obj);
        }
    }

    public class BooleanValue : AbstractNode, IValue
    {
        public BooleanValue(bool value)
        {
            Value = value;
        }

        public bool Value { get; }

        public override string ToString()
        {
            return "BooleanValue{{value={0}}}".ToFormat(Value);
        }

        protected bool Equals(BooleanValue other)
        {
            return Value == other.Value;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((BooleanValue)obj);
        }
    }

    public class DateTimeValue : AbstractNode, IValue
    {
        public DateTimeValue(DateTime value)
        {
            Value = value;
        }

        public DateTime Value { get; }

        public override string ToString()
        {
            return "DateTimeValue{{value={0}}}".ToFormat(Value.ToString("o"));
        }

        protected bool Equals(DateTimeValue other)
        {
            return DateTime.Equals(Value, other.Value);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((DateTimeValue)obj);
        }
    }

    public class EnumValue : AbstractNode, IValue
    {
        public EnumValue(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        public EnumValue(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public NameNode NameNode { get; }

        public override string ToString()
        {
            return "EnumValue{{name={0}}}".ToFormat(Name);
        }

        protected bool Equals(EnumValue other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((EnumValue)obj);
        }
    }

    public class ListValue : AbstractNode, IValue
    {
        public ListValue(IEnumerable<IValue> values)
        {
            Values = values;
        }

        public IEnumerable<IValue> Values { get; }

        public override IEnumerable<INode> Children => Values;

        public override string ToString()
        {
            return "ListValue{{values={0}}}".ToFormat(string.Join(", ", Values.Select(x=>x.ToString())));
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return true;
        }
    }

    public class ObjectValue : AbstractNode, IValue
    {
        public ObjectValue(IEnumerable<ObjectField> fields)
        {
            ObjectFields = fields;
        }

        public IEnumerable<ObjectField> ObjectFields { get; }

        public IEnumerable<string> FieldNames
        {
            get { return ObjectFields.Select(x => x.Name).ToList(); }
        }

        public override IEnumerable<INode> Children => ObjectFields;

        public ObjectField Field(string name)
        {
            return ObjectFields.FirstOrDefault(x => x.Name == name);
        }

        public override string ToString()
        {
            return "ObjectValue{{objectFields={0}}}".ToFormat(string.Join(", ", ObjectFields.Select(x => x.ToString())));
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return true;
        }
    }

    public class ObjectField : AbstractNode
    {
        public ObjectField(NameNode name, IValue value)
            : this(name.Name, value)
        {
            NameNode = name;
        }

        public ObjectField(string name, IValue value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public NameNode NameNode { get; }
        public IValue Value { get; }

        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        public override string ToString()
        {
            return "ObjectField{{name='{0}', value={1}}}".ToFormat(Name, Value);
        }

        protected bool Equals(ObjectField other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((ObjectField) obj);
        }
    }
}
