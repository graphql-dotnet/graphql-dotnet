import React from 'react';
import GraphiQL from 'graphiql';
import fetch from 'isomorphic-fetch';

function graphQLFetcher(graphQLParams) {
  return fetch(window.location.origin + '/api/graphql', {
    method: 'post',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(graphQLParams),
  }).then(response => response.json());
}

class App extends React.Component {
  render() {
    return (
      <GraphiQL fetcher={graphQLFetcher}/>
    );
  }
}

React.render(<GraphiQL fetcher={graphQLFetcher}/>, document.body);
