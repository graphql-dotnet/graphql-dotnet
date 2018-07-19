import React, { Fragment } from 'react'
import Link from 'gatsby-link'
import classnames from 'classnames'

import styles from './SideNav.module.css'
console.log('styles', styles)

const createMenuList = (listData, location) => (
  console.log(location) ||
  <nav>
    <ul className={styles.sideList}>
      {listData.map( element => (
        <li key={element.title}>
          {element.file && (
            <Link
              to={element.url || element.href}
              className={classnames([
                styles.subNav_link,
                location.pathname === element.url && styles.active
              ])}
            >
              {element.title}
            </Link>
          )}
          {!element.file && (
            <span className={styles.subNav_title}>{element.title}</span>
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

export default SideNav
