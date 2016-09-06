import fs from 'fs';
import builder from 'xmlbuilder';

function escapeRegExp(str) {
    return str.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, '\\$1');
}

function replaceAll(str, find, replace) {
  return str.replace(new RegExp(escapeRegExp(find), 'g'), replace);
}

export default (options) => {
  console.log('Writing nuspec');

  let xml = builder.create({
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
  .end({ pretty: true });

  xml = replaceAll(xml, '&lt;', '<');
  xml = replaceAll(xml, '&gt;', '>');
  xml = replaceAll(xml, '&#xD;', '');

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
