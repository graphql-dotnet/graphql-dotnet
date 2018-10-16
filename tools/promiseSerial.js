const promiseSerial = ({ tasks, params }) =>{
  return tasks.reduce((promise, func) =>
    promise.then(result => func(params).then(Array.prototype.concat.bind(result))),
    Promise.resolve([]))
}

export default promiseSerial
