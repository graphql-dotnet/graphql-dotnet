using Fixie;

namespace GraphQL.Tests
{

    public class CustomConvention : Convention
    {
        public CustomConvention()
        {
            Classes
                .NameEndsWith("Tests");

            Methods
                .HasOrInherits<FactAttribute>();
        }
    }
}
