using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Human : AotAutoRegisteringObjectGraphType<Human>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_Human()
        {
            _members = new()
            {
                { typeof(Human).GetProperty(nameof(Human.Id))!, ConstructField_Id },
                { typeof(Human).GetProperty(nameof(Human.Name))!, ConstructField_Name },
                // Friends property is marked with [Ignore], so no field is generated
                { typeof(Human).GetMethod(nameof(Human.GetFriends), [typeof(StarWarsData)])!, ConstructField_GetFriends },
                { typeof(Human).GetMethod(nameof(Human.GetFriendsConnection), [typeof(StarWarsData)])!, ConstructField_GetFriendsConnection },
                { typeof(Human).GetProperty(nameof(Human.AppearsIn))!, ConstructField_AppearsIn },
                // Cursor property is marked with [Ignore], so no field is generated
                { typeof(Human).GetProperty(nameof(Human.HomePlanet))!, ConstructField_HomePlanet },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_Id(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Id, false);
        }

        public void ConstructField_Name(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Name, false);
        }

        public void ConstructField_GetFriends(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var source = GetMemberInstance(context);
                var arg0 = param0(context);
                return source.GetFriends(arg0);
            }, true);
        }

        public void ConstructField_GetFriendsConnection(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var source = GetMemberInstance(context);
                var arg0 = param0(context);
                return source.GetFriendsConnection(arg0);
            }, true);
        }

        public void ConstructField_AppearsIn(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).AppearsIn, false);
        }

        public void ConstructField_HomePlanet(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).HomePlanet, false);
        }
    }
}
