import React from 'react';
import ReactDOM from 'react-dom';
import GraphiQL from 'graphiql';
import axios from 'axios';
import 'graphiql/graphiql.css';
import './app.css';

function graphQLFetcher(graphQLParams) {
  return axios({
    method: 'POST',
    url: window.location.origin + '/api/graphql',
    data: graphQLParams
  }).then(resp => resp.data);
}

ReactDOM.render(<GraphiQL fetcher={graphQLFetcher}/>, document.getElementById('app'));
