using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class Being : InterfaceGraphType
{
    public Being()
    {
        Field<StringGraphType>("name")
            .Argument<BooleanGraphType>("surname");
    }
}

public class Pet : InterfaceGraphType
{
    public Pet()
    {
        Name = "Pet";
        Field<StringGraphType>("name")
            .Argument<BooleanGraphType>("surname");
    }
}

public class Canine : InterfaceGraphType
{
    public Canine()
    {
        Field<StringGraphType>("name")
            .Argument<BooleanGraphType>("surname");
    }
}

public class DogCommand : EnumerationGraphType
{
    public DogCommand()
    {
        Name = "DogCommand";
        Add("SIT", 0);
        Add("HEAL", 1);
        Add("DOWN", 2);
    }
}

public class Dog : ObjectGraphType
{
    public Dog()
    {
        Field<StringGraphType>("name")
            .Argument<StringGraphType>("surname");
        Field<StringGraphType>("nickname");
        Field<BooleanGraphType>("barks");
        Field<IntGraphType>("barkVolume");
        Field<BooleanGraphType>("doesKnowCommand")
            .Argument<DogCommand>("dogCommand");
        Field<BooleanGraphType>("isHousetrained")
            .Argument<BooleanGraphType>("atOtherHomes", arg => arg.DefaultValue = true);
        Interface<Being>();
        Interface<Pet>();
        Interface<Canine>();

        IsTypeOf = _ => true;
    }
}

public class FurColor : EnumerationGraphType
{
    public FurColor()
    {
        Add("Brown", 0);
        Add("Yellow", 1);
    }
}

public class Cat : ObjectGraphType
{
    public Cat()
    {
        Field<StringGraphType>("name")
            .Argument<StringGraphType>("surname");
        Field<StringGraphType>("nickname");
        Field<BooleanGraphType>("meows");
        Field<IntGraphType>("meowVolume");
        Field<FurColor>("furColor");
        Interface<Being>();
        Interface<Pet>();

        IsTypeOf = _ => true;
    }
}

public class CatOrDog : UnionGraphType
{
    public CatOrDog()
    {
        Type<Cat>();
        Type<Dog>();
        ResolveType = value => null;
    }
}

public class Intelligent : InterfaceGraphType
{
    public Intelligent()
    {
        Field<IntGraphType>("id");
    }
}

public class Human : ObjectGraphType
{
    public Human()
    {
        Field<StringGraphType>("name")
            .Argument<BooleanGraphType>("surname");
        Field<ListGraphType<Pet>>("pets");
        Field<ListGraphType<Human>>("relatives");
        Field<IntGraphType>("id");

        Interface<Being>();
        Interface<Intelligent>();

        IsTypeOf = _ => true;
    }
}

public class Alien : ObjectGraphType
{
    public Alien()
    {
        Field<StringGraphType>("name")
            .Argument<BooleanGraphType>("surname");
        Field<ListGraphType<Pet>>("pets");
        Field<ListGraphType<Human>>("relatives");
        Field<IntGraphType>("id");
        Field<IntGraphType>("numEyes");

        Interface<Being>();
        Interface<Intelligent>();

        IsTypeOf = _ => true;
    }
}

public class DogOrHuman : UnionGraphType
{
    public DogOrHuman()
    {
        Type<Dog>();
        Type<Human>();
    }
}

public class HumanOrAlien : UnionGraphType
{
    public HumanOrAlien()
    {
        Type<Human>();
        Type<Alien>();
    }
}

public class ComplexInput : InputObjectGraphType
{
    public ComplexInput()
    {
        Name = "ComplexInput";
        Field<NonNullGraphType<BooleanGraphType>>("requiredField");
        Field<IntGraphType>("intField");
        Field<StringGraphType>("stringField").Directive("length", "min", 3, "max", 7);
        Field<BooleanGraphType>("booleanField");
        Field<ListGraphType<StringGraphType>>("stringListField");
    }
}

public class ComplexInput2 : InputObjectGraphType
{
    public ComplexInput2()
    {
        Name = "ComplexInput2";
        Field<NonNullGraphType<BooleanGraphType>>("requiredField");
        Field<IntGraphType>("intField");
        Field<NonNullGraphType<StringGraphType>>("stringField").Directive("length", "min", 3, "max", 7);
        Field<BooleanGraphType>("booleanField");
        Field<ListGraphType<StringGraphType>>("stringListField");
    }
}

public class ComplicatedArgs : ObjectGraphType
{
    public ComplicatedArgs()
    {
        Name = "ComplicatedArgs";
        Field<StringGraphType>("noArgsField");
        Field<StringGraphType>("intArgField")
            .Argument<IntGraphType>("intArg");
        Field<StringGraphType>("nonNullIntArgField")
            .Argument<NonNullGraphType<IntGraphType>>("nonNullIntArg");
        Field<StringGraphType>("stringArgField")
            .Argument<StringGraphType>("stringArg");
        Field<StringGraphType>("booleanArgField")
            .Argument<BooleanGraphType>("booleanArg");
        Field<StringGraphType>("enumArgField")
            .Argument<FurColor>("enumArg");
        Field<StringGraphType>("floatArgField")
            .Argument<FloatGraphType>("floatArg");
        Field<StringGraphType>("idArgField")
            .Argument<IdGraphType>("idArg");
        Field<StringGraphType>("stringListArgField")
            .Argument<ListGraphType<StringGraphType>>("stringListArg");
        Field<StringGraphType>("complexArgField")
            .Argument<ComplexInput>("complexArg");
        Field<StringGraphType>("complexArgField2")
            .Argument<NonNullGraphType<ComplexInput2>>("complexArg");
        Field<StringGraphType>("multipleReqs")
            .Argument<NonNullGraphType<IntGraphType>>("req1")
            .Argument<NonNullGraphType<IntGraphType>>("req2");
        Field<StringGraphType>("multipleOpts")
            .Argument<IntGraphType>("req1", arg => arg.DefaultValue = 0)
            .Argument<IntGraphType>("req2", arg => arg.DefaultValue = 0);
        Field<StringGraphType>("multipleOptAndReq")
            .Argument<NonNullGraphType<IntGraphType>>("req1")
            .Argument<NonNullGraphType<IntGraphType>>("req2")
            .Argument<IntGraphType>("req3", arg => arg.DefaultValue = 0)
            .Argument<IntGraphType>("req4", arg => arg.DefaultValue = 0);
    }
}

public class ValidationQueryRoot : ObjectGraphType
{
    public ValidationQueryRoot()
    {
        Field<Human>("human")
            .Argument<IdGraphType>("id", arg => arg.ApplyDirective("length", "min", 2, "max", 5));
        Field<Human>("human2")
            .Argument<NonNullGraphType<IdGraphType>>("id", arg => arg.ApplyDirective("length", "min", 2, "max", 5));
        Field<Dog>("dog");
        Field<Cat>("cat");
        Field<CatOrDog>("catOrDog");
        Field<DogOrHuman>("dogOrHuman");
        Field<HumanOrAlien>("humanOrAlien");
        Field<ComplicatedArgs>("complicatedArgs");
    }
}

public class ValidationSchema : Schema
{
    public ValidationSchema()
    {
        Query = new ValidationQueryRoot();
        this.RegisterType<Dog>();
        this.RegisterType<Cat>();
        this.RegisterType<Human>();
        this.RegisterType<Alien>();

        Directives.Register(
            new Directive("onQuery", DirectiveLocation.Query),
            new Directive("onMutation", DirectiveLocation.Mutation),
            new Directive("directiveA", DirectiveLocation.Field),
            new Directive("directiveB", DirectiveLocation.Field),
            new Directive("directive", DirectiveLocation.Field),
            new Directive("rep", DirectiveLocation.Field) { Repeatable = true },

            new LengthDirective()
        );
    }
}
