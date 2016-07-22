export default class Deferred {
  constructor() {
    this.promise = new Promise((resolve, reject)=> {
      this.resolve = resolve;
      this.reject = reject;
    });
  }
}
