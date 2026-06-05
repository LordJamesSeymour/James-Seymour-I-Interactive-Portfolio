# GitHub Pages Deployment

GitHub Pages should serve this project from `main` using the `/docs` folder.

Before pushing a new WebGL build, check that `docs` still contains:

- `.nojekyll`
- `CNAME`
- `index.html`
- `Build/docs.loader.js`
- `Build/docs.data.unityweb`
- `Build/docs.framework.js.unityweb`
- `Build/docs.wasm.unityweb`
- `TemplateData/`

The current Unity WebGL settings are compatible with GitHub Pages:

- Compression format: gzip
- Decompression fallback: enabled
- Data caching: enabled

If Unity rebuilds the folder, make sure it does not delete `docs/CNAME` or `docs/.nojekyll`. The `Test` branch currently does not contain `docs`; publish from `main`, or merge the current `main/docs` folder into any branch you plan to use for Pages.
