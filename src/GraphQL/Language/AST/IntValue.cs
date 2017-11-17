using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GraphQL.Language.AST
{
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        public T Value { get; protected set; }

        object IValue.Value => Value;

        public override string ToString()
        {
            return $"{GetType().Name}{{value={Value}}}";
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((T) obj);
        }

        protected abstract bool Equals(ValueNode<T> node);
    }

    public class IntValue : ValueNode<int>
    {
        public IntValue(int value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<int> other)
        {
            return Value == other.Value;
        }
    }

    public class LongValue : ValueNode<long>
    {
        public LongValue(long value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<long> other)
        {
            return Value == other.Value;
        }
    }

    public class DecimalValue : ValueNode<decimal>
    {
        public DecimalValue(decimal value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<decimal> other)
        {
            return Value == other.Value;
        }
    }

    public class FloatValue : ValueNode<double>
    {
        public FloatValue(double value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<double> other)
        {
            return Value == other.Value;
        }
    }

    public class StringValue : ValueNode<string>
    {
        public StringValue(string value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<string> other)
        {
            return string.Equals(Value, other.Value);
        }
    }

    public class BooleanValue : ValueNode<bool>
    {
        public BooleanValue(bool value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<bool> other)
        {
            return Value == other.Value;
        }
    }

    public class DateTimeValue : ValueNode<DateTime>
    {
        public DateTimeValue(DateTime value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<DateTime> other)
        {
            return DateTime.Equals(Value, other.Value);
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

        object IValue.Value => Name;
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

        public object Value
        {
            get
            {
                return Values.Select(x => x.Value).ToList();
            }
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

        public object Value
        {
            get
            {
                var obj = new Dictionary<string, object>();
                FieldNames.Apply(name => obj.Add(name, Field(name).Value.Value));
                return obj;
            }
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
