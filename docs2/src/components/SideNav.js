import React from 'react'
import PropTypes from 'prop-types'
import Link from 'gatsby-link'
import classnames from 'classnames'

const createMenuList = (listData, location) => (
  <nav>
    <ul>
      {listData.map( element => (
        <li key={element.title}>
          {element.file && (
            <Link
              to={element.url || element.href}
              className={classnames([
                location.pathname === element.url && 'active'
              ])}
            >
              {element.title}
            </Link>
          )}
          {!element.file && (
            <span>{element.title}</span>
          )}
          {element.items && element.items.length > 0 && createMenuList(element.items, location)}
        </li>
      ))}
    </ul>
  </nav>
)

const SideNav = ({ activeItem, location }) => (
  <div className="nav">
    {createMenuList(activeItem.sidemenu, location)}
  </div>
)

SideNav.propTypes = {
  activeItem: PropTypes.shape({
    sidemenu: PropTypes.array
  }),
  location: PropTypes.object
}

export default SideNav
