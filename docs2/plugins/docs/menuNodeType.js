const GraphQLJson = require('graphql-type-json')

module.exports = ({ type }) => {
  if (type.name !== 'DocsMenu') {
    return
  }
  return {
    pages: { type: GraphQLJson }
  }
}
