import React, { Fragment } from 'react'
import PropTypes from 'prop-types'
import Helmet from 'react-helmet'
import { graphql, useStaticQuery } from 'gatsby'

import './reset.css'
import 'prismjs/themes/prism-solarizedlight.css'

import Header from './header'
import SideNav from './SideNav'
import './layout.css'

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

const hasMenu = config => config && config.sidemenu && config.sidemenu.length > 0

const Layout = ({ children, location }) => {
  const data = useStaticQuery(graphql`
    query SiteTitleQuery {
      site {
        siteMetadata {
          title
          description
          keywords
          githubUrl
        }
      }

      menu: docsMenu {
        pages
      }
    }
  `)
  const pathName = getPathName(location)
  const pageConfig = findMatchingPage(data.menu.pages, pathName)
  const nav = hasMenu(pageConfig) ? <SideNav activeItem={pageConfig} pathName={pathName} /> : null
  return (
    <Fragment>
      <Helmet
        title={data.site.siteMetadata.title}
        meta={[
          { name: 'description', content: data.site.siteMetadata.description },
          { name: 'keywords', content: data.site.siteMetadata.keywords }
        ]}
      />
      <Header
        siteTitle={data.site.siteMetadata.title}
        links={data.menu.pages}
        githubUrl={data.site.siteMetadata.githubUrl} />
      <div className="page-body">
        {children}
        {nav}
      </div>
    </Fragment>
  )
}

Layout.propTypes = {
  children: PropTypes.object,
  location: PropTypes.shape({
    pathname: PropTypes.string
  }),
  data: PropTypes.object
}

export default Layout
