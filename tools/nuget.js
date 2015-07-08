import path from 'path';
import yargs from 'yargs';
import { exec } from 'child-process-promise';

const options = yargs
  .alias('d', 'debug')
  .argv;

let releaseConfig = options.debug ? 'debug' : 'release';

let ilRepack = '"packages/ILRepack.2.0.2/tools/ILRepack.exe"';

let outputDir = 'nuget/lib';
let inputDir = `src/GraphQL/bin/${releaseConfig}`;
let outputName = 'GraphQL.dll';
let sources = ['GraphQL.dll', 'Antlr4.Runtime.dll', 'Newtonsoft.Json.dll'];
let out = `/out:${path.join(outputDir, outputName)}`;

let sourceOptions = sources.map(source=> {
  return path.join(inputDir, source);
}).join(' ');

let ilRepackCommand = `${ilRepack} ${out} ${sourceOptions}`;

console.log(ilRepackCommand);

exec(ilRepackCommand)
    .then(function (result) {
        console.log(result.stdout);
        return exec('nuget pack nuget/GraphQL.nuspec -OutputDirectory nuget');
    })
    .then(function(result){
      console.log(result.stdout);
    })
    .fail(function (err) {
        console.error('ERROR: ', err);
    });
