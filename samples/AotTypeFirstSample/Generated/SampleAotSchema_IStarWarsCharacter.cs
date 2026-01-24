using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_IStarWarsCharacter : AotAutoRegisteringInterfaceGraphType<IStarWarsCharacter>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_IStarWarsCharacter()
        {
            _members = new()
            {
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Id))!, ConstructField_Id },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Name))!, ConstructField_Name },
                // Friends property is marked with [Ignore], so no field is generated
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriends), [typeof(StarWarsData)])!, ConstructField_GetFriends },
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriendsConnection), [typeof(StarWarsData)])!, ConstructField_GetFriendsConnection },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.AppearsIn))!, ConstructField_AppearsIn },
                // Cursor property is marked with [Ignore], so no field is generated
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
        {
            _members[memberInfo](fieldType, memberInfo);
            if (!fieldType.IsPrivate)
            {
                fieldType.Resolver = null;
                fieldType.StreamResolver = null;
            }
        }

        public void ConstructField_Id(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "The id of the character.";
        }

        public void ConstructField_Name(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "The name of the character.";
        }

        public void ConstructField_GetFriends(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
        }

        public void ConstructField_GetFriendsConnection(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
        }

        public void ConstructField_AppearsIn(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "Which movie they appear in.";
        }
    }
}
