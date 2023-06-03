import React from 'react'
import PropTypes from 'prop-types'
import logo from './logo.svg'
import './landing.css'
import Layout from '../../src/components/layout'

const LandingPage = () => (
  <div className="landing">
    <img src={logo}/>
    <h1>GraphQL .NET</h1>
  </div>
)

const LayoutWrapper = (props) => (
  <Layout location={props.location}>
    <LandingPage/>
  </Layout>
)

LayoutWrapper.propTypes = {
  location: PropTypes.object
}

export default LayoutWrapper
