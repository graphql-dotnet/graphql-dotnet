import React from 'react'
import Link from 'gatsby-link'

const buildLinks = links => {
  return links.map(link => (
    <li key={link.url}>
      <Link to={link.url}>{link.title}</Link>
    </li>
  ))
}

const Header = ({ siteTitle, links }) => (
  <div
    style={{
      marginBottom: '1.45rem'
    }}
  >
    <div
      style={{
        padding: '1.45rem 1.0875rem'
      }}
    >
      <Link
        to="/"
        style={{
          color: '#2c3e50',
          textDecoration: 'none'
        }}
      >
        {siteTitle}
      </Link>
      <nav>
        <ul>
        {buildLinks(links)}
        </ul>
      </nav>
    </div>
  </div>
)

export default Header
