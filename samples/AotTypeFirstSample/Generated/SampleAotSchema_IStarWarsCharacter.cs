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
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>?> _members;
        public AutoOutputGraphType_IStarWarsCharacter()
        {
            // Do not build resolvers for instance methods on interface types - so pass null actions
            _members = new()
            {
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Id))!, null },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Name))!, null },
                // Friends property is marked with [Ignore], so no field is generated
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriends), [typeof(StarWarsData)])!, null },
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriendsConnection), [typeof(StarWarsData)])!, null },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.AppearsIn))!, null },
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
            _members[memberInfo]?.Invoke(fieldType, memberInfo);
            // interface types cannot have resolvers; private fields (built from static methods) are used for federation resolvers
            if (!fieldType.IsPrivate)
            {
                fieldType.Resolver = null;
                fieldType.StreamResolver = null;
            }
        }
    }
}
