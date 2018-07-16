import React from 'react'
import Link from 'gatsby-link'

const Header = ({ siteTitle }) => (
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
    </div>
  </div>
)

export default Header
