import React from 'react'
import Link from 'gatsby-link'

import styles from './header.module.css'

const buildLinks = links => {
  return links.map(link => (
    <li key={link.url}>
      <Link to={link.url}>{link.title}</Link>
    </li>
  ))
}

const Header = ({ siteTitle, links }) => (
  <nav className="header">
    <ul >
      <li key={'/'}>
        <Link
          to="/">
          {siteTitle}
        </Link>
      </li>
      {buildLinks(links)}
    </ul>
  </nav>
)

export default Header
