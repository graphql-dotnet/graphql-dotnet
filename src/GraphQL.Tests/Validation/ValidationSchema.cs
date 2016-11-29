using GraphQL.Types;

namespace GraphQL.Tests.Validation
{
    public class Being : InterfaceGraphType
    {
        public Being()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> { Name = "surname"}
                ));
        }
    }

    public class Pet : InterfaceGraphType
    {
        public Pet()
        {
            Name = "Pet";
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> { Name = "surname"}
                ));
        }
    }

    public class Canine : InterfaceGraphType
    {
        public Canine()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> { Name = "surname"}
                ));
        }
    }

    public class DogCommand : EnumerationGraphType
    {
        public DogCommand()
        {
            Name = "DogCommand";
            AddValue("SIT", "", 0);
            AddValue("HEAL", "", 1);
            AddValue("DOWN", "", 2);
        }
    }

    public class Dog : ObjectGraphType
    {
        public Dog()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(new QueryArgument<StringGraphType> {Name = "surname"}));
            Field<StringGraphType>("nickname");
            Field<BooleanGraphType>("barks");
            Field<IntGraphType>("barkVolume");
            Field<BooleanGraphType>(
                "doesKnowCommand",
                arguments: new QueryArguments(new QueryArgument<DogCommand> {Name = "dogCommand"}));
            Field<BooleanGraphType>(
                "isHousetrained",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> {Name = "atOtherHomes", DefaultValue = true}));
            Interface<Being>();
            Interface<Pet>();
            Interface<Canine>();

            IsTypeOf = obj => true;
        }
    }

    public class FurColor : EnumerationGraphType
    {
        public FurColor()
        {
            AddValue("Brown", "", 0);
            AddValue("Yellow", "", 1);
        }
    }

    public class Cat : ObjectGraphType
    {
        public Cat()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(new QueryArgument<StringGraphType> {Name = "surname"}));
            Field<StringGraphType>("nickname");
            Field<BooleanGraphType>("meows");
            Field<IntGraphType>("meowVolume");
            Field<FurColor>("furColor");
            Interface<Being>();
            Interface<Pet>();

            IsTypeOf = obj => true;
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
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> {Name = "surname"}
                ));
            Field<ListGraphType<Pet>>("pets");
            Field<ListGraphType<Human>>("relatives");
            Field<IntGraphType>("iq");

            Interface<Being>();
            Interface<Intelligent>();

            IsTypeOf = obj => true;
        }
    }

    public class Alien : ObjectGraphType
    {
        public Alien()
        {
            Field<StringGraphType>(
                "name",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> {Name = "surname"}
                ));
            Field<ListGraphType<Pet>>("pets");
            Field<ListGraphType<Human>>("relatives");
            Field<IntGraphType>("iq");
            Field<IntGraphType>("numEyes");

            Interface<Being>();
            Interface<Intelligent>();

            IsTypeOf = obj => true;
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
            Field<StringGraphType>("stringField");
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
            Field<StringGraphType>(
                "intArgField",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> {Name = "intArg"}
                ));
            Field<StringGraphType>(
                "nonNullIntArgField",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "nonNullIntArg"}
                ));
            Field<StringGraphType>(
                "stringArgField",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> {Name = "stringArg"}
                ));
            Field<StringGraphType>(
                "booleanArgField",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType> {Name = "booleanArg"}
                ));
            Field<StringGraphType>(
                "enumArgField",
                arguments: new QueryArguments(
                    new QueryArgument<FurColor> {Name = "enumArg"}
                ));
            Field<StringGraphType>(
                "floatArgField",
                arguments: new QueryArguments(
                    new QueryArgument<FloatGraphType> {Name = "floatArg"}
                ));
            Field<StringGraphType>(
                "idArgField",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType> {Name = "idArg"}
                ));
            Field<StringGraphType>(
                "stringListArgField",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<StringGraphType>> {Name = "stringListArg"}
                ));
            Field<StringGraphType>(
                "complexArgField",
                arguments: new QueryArguments(
                    new QueryArgument<ComplexInput> { Name = "complexArg"}
                ));
            Field<StringGraphType>(
                "multipleReqs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "req1"},
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "req2"}
                ));
            Field<StringGraphType>(
                "multipleOpts",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> {Name = "req1", DefaultValue = 0},
                    new QueryArgument<IntGraphType> {Name = "req2", DefaultValue = 0}
                ));
            Field<StringGraphType>(
                "multipleOptAndReq",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "req1"},
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "req2"},
                    new QueryArgument<IntGraphType> {Name = "req1", DefaultValue = 0},
                    new QueryArgument<IntGraphType> {Name = "req2", DefaultValue = 0}
                ));
        }
    }

    public class ValidationQueryRoot : ObjectGraphType
    {
        public ValidationQueryRoot()
        {
            Field<Human>(
                "human",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType>
                    {
                        Name = "id"
                    }
                ));
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
            RegisterType<Dog>();
            RegisterType<Cat>();
            RegisterType<Human>();
            RegisterType<Alien>();

            Directives = new []
            {
                DirectiveGraphType.Include,
                DirectiveGraphType.Skip,
                DirectiveGraphType.Deprecated,
                new DirectiveGraphType("onQuery", new []{ DirectiveLocation.Query }),
                new DirectiveGraphType("onMutation", new []{ DirectiveLocation.Mutation }),
                new DirectiveGraphType("onSubscription", new []{ DirectiveLocation.Subscription }),
                new DirectiveGraphType("onField", new []{ DirectiveLocation.Field }),
                new DirectiveGraphType("onFragmentDefinition", new []{ DirectiveLocation.FragmentDefinition }),
                new DirectiveGraphType("onFragmentSpread", new []{ DirectiveLocation.FragmentSpread }),
                new DirectiveGraphType("onInlineFragment", new []{ DirectiveLocation.InlineFragment }),
                new DirectiveGraphType("onSchema", new []{ DirectiveLocation.Schema }),
                new DirectiveGraphType("onScalar", new []{ DirectiveLocation.Scalar }),
                new DirectiveGraphType("onObject", new []{ DirectiveLocation.Object }),
                new DirectiveGraphType("onFieldDefinition", new []{ DirectiveLocation.FieldDefinition }),
                new DirectiveGraphType("onArgumentDefinition", new []{ DirectiveLocation.ArgumentDefinition }),
                new DirectiveGraphType("onInterface", new []{ DirectiveLocation.Interface }),
                new DirectiveGraphType("onUnion", new []{ DirectiveLocation.Union }),
                new DirectiveGraphType("onEnum", new []{ DirectiveLocation.Enum }),
                new DirectiveGraphType("onEnumValue", new []{ DirectiveLocation.EnumValue }),
                new DirectiveGraphType("onInputObject", new []{ DirectiveLocation.InputObject }),
                new DirectiveGraphType("onInputFieldDefinition", new []{ DirectiveLocation.InputFieldDefinition })
            };
        }
    }
}
