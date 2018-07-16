import React from 'react'
import PropTypes from 'prop-types'
import Helmet from 'react-helmet'

import 'prismjs/themes/prism-solarizedlight.css'

import Header from '../components/header'
import SideNav from '../components/SideNav'
import './reset.css'
import './index.css'

import { findMatchingPage  } from '../utils/navigation'

const getPathName = (location, pathNamePrefix = '') => {
  let pathName = location.pathname

  if (pathName === '/') {
    return pathName
  }

  if (pathNamePrefix && pathNamePrefix.trim() && pathName.startsWith(pathNamePrefix)) {
    pathName = pathName.substring(pathNamePrefix.length, pathName.length)
  }

  if (pathName.substring(pathName.length-1) === '/') {
    pathName = pathName.substring(0, pathName.length-1)
  }

  return pathName
}

const Layout = ({ children, location, data }) =>{
  const pathName = getPathName(location)
  const pageConfig = findMatchingPage(data.menu.pages, pathName)
  const nav = pageConfig ? <SideNav activeItem={pageConfig} location={location} /> : null

  return (
    <div>
      <Helmet
        title={data.site.siteMetadata.title}
        meta={[
          { name: 'description', content: 'Sample' },
          { name: 'keywords', content: 'sample, something' }
        ]}
      />
      <Header siteTitle={data.site.siteMetadata.title} links={data.menu.pages} />
      <div style={{ float: 'left' }}>
      {nav}
      </div>
      <div
        style={{
          float: 'left',
          margin: '0 auto',
          maxWidth: 960,
          padding: '0px 1.0875rem 1.45rem',
          paddingTop: 0
        }}
      >
        {children()}
      </div>
    </div>
  )
}

Layout.propTypes = {
  children: PropTypes.func,
  location: PropTypes.shape({
    pathname: PropTypes.string
  }),
  data: PropTypes.object
}

export default Layout

export const query = graphql`
  query SiteTitleQuery {
    site {
      siteMetadata {
        title
      }
    }

    menu: docsMenu {
      pages
    }
  }
`
