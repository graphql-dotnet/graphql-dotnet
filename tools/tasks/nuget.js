import fs from 'fs';
import path from 'path';
import rimraf from 'rimraf';
import { exec } from 'child-process-promise';
import Deferred from './Deferred';

export default (options) => {

  console.log('ILMerging');

  let releaseConfig = options.target;

  const ilMerge = path.resolve('./ILMerge.exe');
  const nuget = path.resolve('./nuget.exe');
  const nuspec = path.resolve('./nuget/GraphQL.nuspec');
  const nugetDir = path.resolve('./nuget');

  const outputDir = 'nuget/lib/net45';
  const inputDir = `src/GraphQL/bin/${releaseConfig}`;
  const outputName = 'GraphQL.dll';
  const sources = ['GraphQL.dll', 'GraphQLParser.dll', 'Antlr4.Runtime.dll', 'Newtonsoft.Json.dll'];
  const outFlag = `/out:${path.join(outputDir, outputName)}`;
  const versionFlag = `/ver:${options.version}`;

  let sourceOptions = sources.map(source => {
    return path.join(inputDir, source);
  }).join(' ');

  if (fs.existsSync(outputDir)) {
    rimraf.sync('nuget/lib');
  }

  fs.mkdirSync('nuget/lib');
  fs.mkdirSync(outputDir);

  let ilRepackCommand = `${ilMerge} ${versionFlag} /target:library /targetplatform:v4 /internalize ${outFlag}  ${sourceOptions}`;

  console.log(ilRepackCommand);

  const deferred = new Deferred();

  exec(ilRepackCommand)
    .then(function (result) {
        console.log(result.stdout);
        return exec(`${nuget} pack ${nuspec} -OutputDirectory ${nugetDir}`);
    })
    .then(function(result){
      console.log(result.stdout);
      deferred.resolve();
    })
    .fail(function (err) {
        console.error('ERROR: ', err.stdout);
        deferred.reject();
    });

  return deferred.promise;
};
