using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_StarWarsQuery : AotAutoRegisteringObjectGraphType<StarWarsQuery>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_StarWarsQuery()
        {
            _members = new()
            {
                { typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.HeroAsync), [typeof(StarWarsData)])!, ConstructField_HeroAsync },
                { typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.HumanAsync), [typeof(StarWarsData), typeof(string)])!, ConstructField_HumanAsync },
                { typeof(StarWarsQuery).GetMethod(nameof(StarWarsQuery.DroidAsync), [typeof(StarWarsData), typeof(string)])!, ConstructField_DroidAsync },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_HeroAsync(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var arg0 = param0(context);
                return StarWarsQuery.HeroAsync(arg0);
            }, true);
        }

        public void ConstructField_HumanAsync(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            var param1 = BuildArgument<string>(fieldType, parameters[1]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var arg0 = param0(context);
                var arg1 = param1(context);
                return StarWarsQuery.HumanAsync(arg0, arg1);
            }, true);
        }

        public void ConstructField_DroidAsync(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            var param1 = BuildArgument<string>(fieldType, parameters[1]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var arg0 = param0(context);
                var arg1 = param1(context);
                return StarWarsQuery.DroidAsync(arg0, arg1);
            }, true);
        }
    }
}
