import GraphQLJSON from 'graphql-type-json'

export default ({ type }) => {
  console.log(type)
  if (type.name !== 'DocsMenu') {
    return
  }
  return {
    pages: { type: GraphQLJSON }
  }
}
