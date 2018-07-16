const GraphQLJson = require('graphql-type-json')

module.exports = ({ type }) => {
  console.log('Types', type.name)
  if (type.name !== 'DocsMenu') {
    return
  }
  return {
    pages: { type: GraphQLJson }
  }
}
