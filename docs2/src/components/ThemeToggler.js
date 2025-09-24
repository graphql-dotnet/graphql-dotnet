import React from 'react'
import { THEMES, useTheme } from '../utils/themeManager'
import { Around } from '@theme-toggles/react'
import css from '@theme-toggles/react/css/Around.css'

/** @see {@link https://github.com/gatsbyjs/gatsby/issues/19446} */
css

export const ThemeToggler = () => {
  const { theme, setTheme } = useTheme()
  const onToggle = (isDark) => {
    setTheme(isDark ? THEMES.DARK : THEMES.LIGHT)
  }

  return (
    <Around
      toggled={theme === THEMES.DARK}
      onToggle={onToggle}
      style={{ fontSize: '2rem' }}
    />
  )
}
