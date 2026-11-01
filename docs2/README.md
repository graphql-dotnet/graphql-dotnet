# GraphQL .NET documentation

The documentation is a static Astro site. Documentation content stays in
`site/docs`, and `site/sitemap.yml` defines the navigation and published paths.
Files in `public` are copied into the generated site.

Use a Node.js version satisfying `engines.node` in `package.json` (currently
**22.23.4 or later**). All documentation workflows read that requirement directly
using `actions/setup-node`'s `node-version-file` input. From this directory:

```sh
npm ci
npm run develop
```

Build the published site with:

```sh
npm run build
npm run preview
```

The build writes to `dist`. Publishing does not require an application server.
The `.nojekyll` file must be included when uploading `dist` to GitHub Pages;
workflow artifacts, the release ZIP, and the manual publishing script retain it.

## Publishing

The documentation workflow publishes `dist` to the `master` branch of
[`graphql-dotnet.github.io`](https://github.com/graphql-dotnet/graphql-dotnet.github.io).
The release workflow also attaches a `Documentation.zip` containing the same
static site. All four workflows that build documentation use `npm ci`.

For manual publication, install the dependencies, ensure you have repository
write access and GitHub SSH authentication, then run:

```sh
npm run deploy -- 8.0.0
```

This builds the site and publishes `dist`, including dotfiles, to the hosting
repository. The version is used in the deployment commit message.
