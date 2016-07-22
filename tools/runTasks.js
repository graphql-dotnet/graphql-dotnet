import chalk from 'chalk';
import moment from 'moment';

export default function runSerial(tasks) {
  const start = moment();
  return new Promise((resolve, reject) => {
    let hasError = false;
    tasks.reduce((cur, next) => {
      if(hasError) {
        return null;
      }
      return cur.then(next);
    }, Promise.resolve()).then(() => {
      resolve({ start });
    }, (error) => {
      // console.log('serial runner');
      hasError = true;
      console.error(chalk.red(error));
      reject({ start, error });
    });
  })
  .then(res => {
    const end = moment();
    const minutes = end.diff(res.start, 'minutes');
    const seconds = end.diff(res.start, 'seconds');
    console.log(chalk.white(`\n------ The build took ${minutes} minutes and ${seconds} seconds. ------`));
    return res;
  }, res => {
    const end = moment();
    const minutes = end.diff(res.start, 'minutes');
    const seconds = end.diff(res.start, 'seconds');
    console.log(chalk.white(`\n------ The build took ${minutes} minutes and ${seconds} seconds. ------`));
    process.exitCode = 1;
    return res;
  });
}
