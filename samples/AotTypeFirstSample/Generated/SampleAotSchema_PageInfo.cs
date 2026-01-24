using System.Reflection;
using GraphQL.Types;
using GraphQL.Types.Aot;
using GraphQL.Types.Relay.DataObjects;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_PageInfo : AotAutoRegisteringObjectGraphType<PageInfo>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_PageInfo()
        {
            _members = new()
            {
                { typeof(PageInfo).GetProperty(nameof(PageInfo.HasNextPage))!, ConstructField_HasNextPage },
                { typeof(PageInfo).GetProperty(nameof(PageInfo.HasPreviousPage))!, ConstructField_HasPreviousPage },
                { typeof(PageInfo).GetProperty(nameof(PageInfo.StartCursor))!, ConstructField_StartCursor },
                { typeof(PageInfo).GetProperty(nameof(PageInfo.EndCursor))!, ConstructField_EndCursor },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField_HasNextPage(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).HasNextPage, false);
        }

        public void ConstructField_HasPreviousPage(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).HasPreviousPage, false);
        }

        public void ConstructField_StartCursor(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).StartCursor, false);
        }

        public void ConstructField_EndCursor(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).EndCursor, false);
        }
    }
}
