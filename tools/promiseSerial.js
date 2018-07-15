import promiseTimeout from 'simple-make/lib/promiseTimeout'

const promiseSerial = ({ tasks, params, taskTimeout }) =>{
  return tasks.reduce((promise, func) =>
    promise.then(result => promiseTimeout(taskTimeout || 5000, func(params)).then(Array.prototype.concat.bind(result))),
    Promise.resolve([]))
}

export default promiseSerial
