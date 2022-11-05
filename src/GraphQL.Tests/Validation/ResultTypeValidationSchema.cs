using GraphQL.Types;

namespace GraphQL.Tests.Validation;

public class ResultTypeValidationSchema : Schema
{
    public ResultTypeValidationSchema()
    {
        Query = new ResultTypeValidationQueryRoot();
        this.RegisterType<IntBox>();
        this.RegisterType<StringBox>();
        this.RegisterType<NonNullStringBox1Imp>();
        this.RegisterType<NonNullStringBox2Imp>();
    }
}

public class ResultTypeValidationQueryRoot : ObjectGraphType
{
    public ResultTypeValidationQueryRoot()
    {
        Field<SomeBox>("someBox");
    }
}

public class SomeBox : InterfaceGraphType
{
    public SomeBox()
    {
        Field<SomeBox>("deepBox");
        Field<StringGraphType>("unrelatedField");
    }
}

public class StringBox : ObjectGraphType
{
    public StringBox()
    {
        Field<StringGraphType>("scalar");
        Field<SomeBox>("deepBox");
        Field<StringGraphType>("unrelatedField");
        Field<ListGraphType<StringBox>>("listStringBox");
        Field<StringBox>("stringBox");
        Field<IntBox>("intBox");

        Interface<SomeBox>();
        IsTypeOf = obj => true;
    }
}

public class IntBox : ObjectGraphType
{
    public IntBox()
    {
        Field<IntGraphType>("scalar");
        Field<SomeBox>("deepBox");
        Field<StringGraphType>("unrelatedField");
        Field<ListGraphType<StringBox>>("listStringBox");
        Field<StringBox>("stringBox");
        Field<IntBox>("intBox");

        Interface<SomeBox>();
        IsTypeOf = obj => true;
    }
}

public class NonNullStringBox1 : InterfaceGraphType
{
    public NonNullStringBox1()
    {
        Field<NonNullGraphType<StringGraphType>>("scalar");
    }
}

public class NonNullStringBox1Imp : ObjectGraphType
{
    public NonNullStringBox1Imp()
    {
        Field<NonNullGraphType<StringGraphType>>("scalar");
        Field<StringGraphType>("unrelatedField");
        Field<SomeBox>("deepBox");

        Interface<SomeBox>();
        Interface<NonNullStringBox1>();
        IsTypeOf = obj => true;
    }
}

public class NonNullStringBox2 : InterfaceGraphType
{
    public NonNullStringBox2()
    {
        Field<NonNullGraphType<StringGraphType>>("scalar");
    }
}

public class NonNullStringBox2Imp : ObjectGraphType
{
    public NonNullStringBox2Imp()
    {
        Field<NonNullGraphType<StringGraphType>>("scalar");
        Field<StringGraphType>("unrelatedField");
        Field<SomeBox>("deepBox");

        Interface<SomeBox>();
        Interface<NonNullStringBox2>();
        IsTypeOf = obj => true;
    }
}
