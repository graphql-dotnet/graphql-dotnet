using System.Reflection;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Edge_IStarWarsCharacter : AotAutoRegisteringObjectGraphType<Edge<IStarWarsCharacter>>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_Edge_IStarWarsCharacter()
        {
            Name = "CharacterEdge";

            _members = new()
            {
                { typeof(Edge<IStarWarsCharacter>).GetProperty(nameof(Edge<IStarWarsCharacter>.Cursor))!, ConstructField_Cursor },
                { typeof(Edge<IStarWarsCharacter>).GetProperty(nameof(Edge<IStarWarsCharacter>.Node))!, ConstructField_Node },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_Cursor(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Cursor, false);
        }

        public void ConstructField_Node(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Node, false);
        }
    }
}
