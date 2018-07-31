import React from 'react'
import PropTypes from 'prop-types'
import Link from 'gatsby-link'

const buildLinks = links => {
  return links.map(link => (
    <li key={link.url}>
      <Link to={link.url}>{link.title}</Link>
    </li>
  ))
}

const Header = ({ siteTitle, links, githubUrl }) => (
  <nav className="header">
    <ul>
      {buildLinks(links)}
      <li key="github-link">
        <a href={githubUrl} rel="noopener noreferrer" target="_blank">GitHub</a>
      </li>
    </ul>
  </nav>
)

Header.propTypes = {
  siteTitle: PropTypes.string,
  links: PropTypes.array,
  githubUrl: PropTypes.string
}

export default Header
