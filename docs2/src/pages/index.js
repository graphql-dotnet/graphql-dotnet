import React from 'react'
import PropTypes from 'prop-types'
import Link from 'gatsby-link'

const IndexPage = ({ data }) => (
  <div>
    {data.allMarkdownRemark.edges.map(({ node }) => (
      <div key={node.id}>
        <Link to={node.fields.slug}>
          <h3>
            {node.frontmatter.title}
          </h3>
        </Link>
        <p>{node.excerpt}</p>
      </div>
    ))}
  </div>
)

IndexPage.propTypes = {
  data: PropTypes.object
}

export const query = graphql`
  query pages {
    allMarkdownRemark {
      totalCount
      edges {
        node {
          id
          frontmatter {
            title
          }
          excerpt
          fields {
            slug
          }
        }
      }
    }
  }
`

export default IndexPage
