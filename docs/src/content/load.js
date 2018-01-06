function appendScript(url) {
  const script = document.createElement('script')
  script.src = url
  document.body.appendChild(script)
}

domready(function() {
  axios.get('/content/manifest.json')
    .then(function(response) {
      Object.keys(response.data).forEach(key =>
        appendScript(response.data[key])
      )
    })
})
