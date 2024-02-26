import React from 'react'
import PropTypes from 'prop-types'
import { Link } from 'gatsby'
import classnames from 'classnames'

const createMenuList = (listData, pathName) => (
  <nav>
    <ul>
      {listData.map( element => (
        <li key={element.title}>
          {element.file && (
            <Link
              to={element.url || element.href}
              className={classnames([
                pathName === element.url && 'active'
              ])}
            >
              {element.title}
            </Link>
          )}
          {!element.file && (
            <span>{element.title}</span>
          )}
          {element.items && element.items.length > 0 && createMenuList(element.items, pathName)}
        </li>
      ))}
    </ul>
  </nav>
)

const SideNav = ({ activeItem, pathName }) => (
  <div className="nav">
    {createMenuList(activeItem.sidemenu, pathName)}
  </div>
)

SideNav.propTypes = {
  activeItem: PropTypes.shape({
    sidemenu: PropTypes.array
  }),
  pathName: PropTypes.string
}

export default SideNav
