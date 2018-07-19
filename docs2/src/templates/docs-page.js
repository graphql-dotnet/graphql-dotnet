import React from 'react'
import PropTypes from 'prop-types'

const DocsPage = ({ data }) => {
  const page = data.markdownRemark
  return (
    <article
      className="content"
      dangerouslySetInnerHTML={{ __html: page.html }} />
  )
}

DocsPage.propTypes = {
  data: PropTypes.object
}

export const query = graphql`
  query DocsPage($relativePath: String!) {
    markdownRemark(fields: { relativePath: { eq: $relativePath } }) {
      html
    }
  }
`

export default DocsPage
