import {
   default as React,
   Component
} from 'react'

class Sidebar extends Component {
  constructor(props) {
    super(props)

    this.state = {
      sections: [
      ]
    }
  }

  componentDidMount() {
    const elements = document.querySelectorAll('#main-pane h2')
    const sections = []

    elements.forEach(e => {
      sections.push({ slug: e.id, title: e.innerHTML })
    })

    this.setState({sections})
  }

  render() {
    const { sections } = this.state
    const items = sections.map((item, i)=> (
      <li key={i} className="nav-item">
        <a href={`#${item.slug}`} className="nav-link">{item.title}</a>
      </li>
    ))
    return (
      <ul className="nav nav-stacked">
        {items}
      </ul>
    )
  }
}

export default Sidebar
