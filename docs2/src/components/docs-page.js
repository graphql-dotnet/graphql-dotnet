import React from 'react'
import PropTypes from 'prop-types'
import { graphql } from 'gatsby'
import Layout from './layout'

const DocsPage = ({data}) => {
  const page = data.markdownRemark
  const metadata = data.site.siteMetadata
  const editUrl = metadata.githubEditUrl + '/' + page.fields.relativePath
  return (
    <div className="content">
      <article
        className="content-body"
        dangerouslySetInnerHTML={{ __html: page.html }} />
      <div className="content-toolbar">
        <a href={editUrl} rel="noopener noreferrer" target="_blank">Edit this page on GitHub</a>
      </div>
    </div>
  )
}


const LayoutWrapper = (props) => {
  return (
    <Layout location={props.location}>
      <DocsPage data = {props.data}/>
    </Layout>
  )
}

LayoutWrapper.propTypes = {
  location: PropTypes.object,
  data: PropTypes.object
}

export const query = graphql`
  query DocsPage($relativePath: String!) {
    markdownRemark(fields: { relativePath: { eq: $relativePath } }) {
      html
      fields {
        relativePath
      }
    }
    site {
      siteMetadata {
        githubEditUrl
      }
    }
  }
`

export default LayoutWrapper
