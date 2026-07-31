const fs = require('fs')
const path = require('path')
const axios = require('axios')

function createImageService({ filesDir, imageUrl, imageRefreshIntervalMs }) {
  const imagePath = path.join(filesDir, 'image.jpg')
  const timestampPath = path.join(filesDir, 'image_timestamp.txt')

  function ensureDirectoryExists() {
    if (!fs.existsSync(filesDir)) {
      fs.mkdirSync(filesDir, { recursive: true })
    }
  }

  async function fetchAndSaveNewImage() {
    try {
      ensureDirectoryExists()
      console.log(`Fetching new image from ${imageUrl}...`)
      const response = await axios.get(imageUrl, { responseType: 'stream' })
      const tempPath = `${imagePath}.tmp`
      const writer = fs.createWriteStream(tempPath)

      response.data.pipe(writer)

      await new Promise((resolve, reject) => {
        writer.on('finish', resolve)
        writer.on('error', reject)
      })

      fs.renameSync(tempPath, imagePath)
      const now = Date.now()
      fs.writeFileSync(timestampPath, now.toString())
      console.log(`Successfully updated image.jpg on persistent volume at ${new Date(now).toISOString()}.`)
      return now
    } catch (err) {
      console.error('Failed to fetch new image:', err.message)
      return Date.now()
    }
  }

  async function getImageInfo() {
    ensureDirectoryExists()
    const exists = fs.existsSync(imagePath)

    if (!exists) {
      const newTimestamp = await fetchAndSaveNewImage()
      return { lastUpdated: newTimestamp }
    }

    let lastUpdated = 0
    if (fs.existsSync(timestampPath)) {
      lastUpdated = parseInt(fs.readFileSync(timestampPath, 'utf-8'), 10) || 0
    } else {
      const stats = fs.statSync(imagePath)
      lastUpdated = Math.floor(stats.mtimeMs)
    }

    if (Date.now() - lastUpdated > imageRefreshIntervalMs) {
      console.log(`Image is older than ${imageRefreshIntervalMs / 1000}s. Triggering background refresh...`)
      void fetchAndSaveNewImage()
    }

    return { lastUpdated }
  }

  async function getImageStream() {
    await getImageInfo()
    return fs.existsSync(imagePath) ? fs.createReadStream(imagePath) : null
  }

  return {
    getImageInfo,
    getImageStream
  }
}

module.exports = createImageService
