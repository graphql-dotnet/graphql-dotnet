import React from 'react'
import PropTypes from 'prop-types'

const DocsPage = ({ data }) => {
  const page = data.markdownRemark
  return (
    <div>
      <h1>{page.frontmatter.title}</h1>
      <div dangerouslySetInnerHTML={{ __html: page.html }} />
    </div>
  )
}

DocsPage.propTypes = {
  data: PropTypes.object
}

export const query = graphql`
  query DocsPage($slug: String!) {
    markdownRemark(fields: { slug: { eq: $slug } }) {
      html
      frontmatter {
        title
      }
    }
  }
`

export default DocsPage
