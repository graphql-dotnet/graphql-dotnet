using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_StarWarsMutation : AotAutoRegisteringObjectGraphType<StarWarsMutation>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_StarWarsMutation()
        {
            _members = new()
            {
                { typeof(StarWarsMutation).GetMethod(nameof(StarWarsMutation.CreateHuman), [typeof(StarWarsData), typeof(HumanInput)])!, ConstructField_CreateHuman },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_CreateHuman(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            var param1 = BuildArgument<HumanInput>(fieldType, parameters[1]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var arg0 = param0(context);
                var arg1 = param1(context);
                return StarWarsMutation.CreateHuman(arg0, arg1);
            }, true);
        }
    }
}
