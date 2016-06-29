import fs from 'fs';
import builder from 'xmlbuilder';

export default (options) => {
  console.log('Writing nuspec');

  const xml = builder.create({
    package: {
      metadata: {
        id: options.id,
        version: options.version,
        authors: options.authors,
        owners: options.owners,
        licenseUrl: options.licenseUrl,
        projectUrl: options.projectUrl,
        requireLicenseAcceptance: false,
        description: options.description,
        releaseNotes: `<![CDATA[${options.releaseNotes}]]>`,
        tags: options.tags
      }
    }
  })
  .end({ pretty: true })
  .replace('&lt;', '<')
  .replace('&gt;', '>');

  // console.log(`\n${xml}\n`);

  return new Promise((resolve, reject) => {
    fs.writeFile(options.file, xml, err =>{
      if (err) {
        reject(err);
      } else {
        resolve();
      }
    });
  });
};
