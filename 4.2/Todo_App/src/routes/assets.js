const fs = require('fs')
const path = require('path')

function registerAssetRoutes(router, { imageService, publicDir }) {
  router.get('/style.css', ctx => {
    const generatedCssPath = path.join(publicDir, 'dist.css')
    const sourceCssPath = path.join(publicDir, 'style.css')

    ctx.type = 'text/css'
    ctx.body = fs.existsSync(generatedCssPath)
      ? fs.createReadStream(generatedCssPath)
      : fs.createReadStream(sourceCssPath)
  })

  router.get('/image.jpg', async ctx => {
    const imageStream = await imageService.getImageStream()
    if (!imageStream) {
      ctx.status = 404
      ctx.body = 'Image loading...'
      return
    }

    ctx.type = 'image/jpeg'
    ctx.body = imageStream
  })
}

module.exports = registerAssetRoutes
