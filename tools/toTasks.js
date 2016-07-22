function tasksFromObject(obj, target) {
  // console.log('target', target);
  if(typeof target === 'function') {
    return target;
  }

  let res = obj[target];

  if(typeof res === 'function') {
    return res;
  }

  let actions = res;

  if(typeof res === 'string') {
    actions = res.split(' ');
  }

  return actions.reduce((cur, next) => {
    return cur.concat(tasksFromObject(obj, next));
  }, []);
}

export default function toTasks(obj, target) {
  return [].concat(tasksFromObject(obj, target));
}
