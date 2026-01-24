using System.Reflection;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Connection_IStarWarsCharacter : AotAutoRegisteringObjectGraphType<Connection<IStarWarsCharacter>>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_Connection_IStarWarsCharacter()
        {
            Name = "CharacterConnection";

            _members = new()
            {
                { typeof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>).GetProperty(nameof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>.TotalCount))!, ConstructField_TotalCount },
                { typeof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>).GetProperty(nameof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>.PageInfo))!, ConstructField_PageInfo },
                { typeof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>).GetProperty(nameof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>.Edges))!, ConstructField_Edges },
                { typeof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>).GetProperty(nameof(GraphQL.Types.Relay.DataObjects.Connection<IStarWarsCharacter>.Items))!, ConstructField_Items },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_TotalCount(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).TotalCount, false);
        }

        public void ConstructField_PageInfo(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).PageInfo, false);
        }

        public void ConstructField_Edges(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Edges, false);
        }

        public void ConstructField_Items(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Items, false);
        }
    }
}
