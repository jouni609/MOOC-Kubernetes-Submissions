const Koa = require('koa')
const fs = require('fs')
const path = require('path')
const axios = require('axios')

const app = new Koa()
const PORT = process.env.PORT || 3000

const directory = path.join('/', 'usr', 'src', 'app', 'files')
const imagePath = path.join(directory, 'image.jpg')
const timestampPath = path.join(directory, 'image_timestamp.txt')

const TEN_MINUTES_MS = 10 * 60 * 1000

function ensureDirectoryExists() {
  if (!fs.existsSync(directory)) {
    fs.mkdirSync(directory, { recursive: true })
  }
}

async function fetchAndSaveNewImage() {
  try {
    ensureDirectoryExists()
    console.log('Fetching new image from Lorem Picsum...')
    const response = await axios.get('https://picsum.photos/1200', { responseType: 'stream' })
    const tempPath = `${imagePath}.tmp`
    const writer = fs.createWriteStream(tempPath)
    
    response.data.pipe(writer)
    
    await new Promise((resolve, reject) => {
      writer.on('finish', resolve)
      writer.on('error', reject)
    })
    
    fs.renameSync(tempPath, imagePath)
    fs.writeFileSync(timestampPath, Date.now().toString())
    console.log('Successfully updated image.jpg on persistent volume.')
  } catch (err) {
    console.error('Failed to fetch new image:', err.message)
  }
}

async function getImageFile() {
  ensureDirectoryExists()
  const exists = fs.existsSync(imagePath)
  
  if (!exists) {
    await fetchAndSaveNewImage()
    return
  }

  // Check if image is older than 10 minutes
  let lastUpdated = 0
  if (fs.existsSync(timestampPath)) {
    lastUpdated = parseInt(fs.readFileSync(timestampPath, 'utf-8'), 10) || 0
  } else {
    const stats = fs.statSync(imagePath)
    lastUpdated = stats.mtimeMs
  }

  if (Date.now() - lastUpdated > TEN_MINUTES_MS) {
    console.log('Image is older than 10 minutes. Serving existing image and fetching new one in background...')
    // Trigger background refresh so user gets current image now, next request gets new image
    fetchAndSaveNewImage()
  }
}

app.use(async ctx => {
  if (ctx.path === '/style.css') {
    ctx.type = 'text/css'
    const cssPath = path.join(__dirname, 'public', 'dist.css')
    if (fs.existsSync(cssPath)) {
      ctx.body = fs.createReadStream(cssPath)
    } else {
      ctx.body = fs.createReadStream(path.join(__dirname, 'public', 'style.css'))
    }
    return
  }

  if (ctx.path === '/image.jpg') {
    await getImageFile()
    if (fs.existsSync(imagePath)) {
      ctx.type = 'image/jpeg'
      ctx.body = fs.createReadStream(imagePath)
    } else {
      ctx.status = 404
      ctx.body = 'Image loading...'
    }
    return
  }

  if (ctx.path.includes('favicon.ico')) return

  // Main Landing Page UI
  await getImageFile()
  ctx.type = 'html'
  ctx.body = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Project - Exercise 1.12</title>
  <link rel="stylesheet" href="/style.css">
</head>
<body class="bg-slate-900 text-slate-100 min-h-screen flex flex-col items-center justify-center p-4">
  <main class="bg-slate-800/90 border border-slate-700/60 rounded-2xl p-6 max-w-xl w-full shadow-2xl backdrop-blur-sm space-y-6">
    
    <div class="text-center">
      <h1 class="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-sky-400 to-blue-500 mb-2 tracking-tight">
        Project - Exercise 1.12
      </h1>
      <p class="text-slate-400 text-sm">Hourly cached image stored on Kubernetes PersistentVolume</p>
    </div>

    <!-- Cached Image Display -->
    <div class="relative rounded-xl overflow-hidden border border-slate-700/60 shadow-lg bg-slate-950">
      <img src="/image.jpg" alt="Hourly Random Image" class="w-full h-72 object-cover transition-opacity duration-300">
      <div class="absolute bottom-3 right-3 px-3 py-1 bg-slate-900/80 backdrop-blur-md rounded-full text-xs font-medium text-sky-400 border border-slate-700/60 shadow-md">
        Cached 10 min
      </div>
    </div>

  </main>
</body>
</html>`
})

console.log(`Server starting on port ${PORT}...`)
app.listen(PORT)
