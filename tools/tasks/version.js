import chalk from 'chalk'
import fs from 'fs'
import { exec } from 'child-process-promise'
import { log, logError } from 'simple-make/lib/logUtils'
import settings from './settings'

function gitCommit() {
  const git = 'git log -1 --pretty=format:%H'
  return exec(git)
    .then(function (result) {
      return result.stdout
    })
    .fail(function (err) {
      logError(chalk.red(err.stdout))
    })
}

export default function version() {

  return gitCommit().then(commit => {

    log('Writing CommonAssemblyInfo.cs\n')

    const options = {
      description: 'GraphQL for .NET',
      productName: 'GraphQL',
      copyright: 'Copyright 2015-2017 Joseph T. McBride et al.  All rights reserved.',
      trademark: commit,
      version: settings.assemblyVersion,
      fileVersion: settings.assemblyVersion,
      informationalVersion: settings.assemblyVersion
    }

    const fileInfo = `using System;
using System.Reflection;
[assembly: AssemblyDescription("${options.description}")]
[assembly: AssemblyTitle("${options.productName}")]
[assembly: AssemblyProduct("${options.productName}")]
[assembly: AssemblyCopyright("${options.copyright}")]
[assembly: AssemblyTrademark("${options.trademark}")]
[assembly: AssemblyVersion("${options.version}")]
[assembly: AssemblyFileVersion("${options.fileVersion}")]
[assembly: AssemblyInformationalVersion("${options.informationalVersion}")]
[assembly: CLSCompliant(false)]`

    return new Promise((resolve, reject) => {
      fs.writeFile('./src/CommonAssemblyInfo.cs', fileInfo, err =>{
        if (err) {
          reject(err)
        } else {
          resolve()
        }
      })
    })
  })
}
