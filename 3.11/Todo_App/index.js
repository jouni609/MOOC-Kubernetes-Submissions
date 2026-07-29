const Koa = require('koa')
const { bodyParser } = require('@koa/bodyparser')
const fs = require('fs')
const path = require('path')
const axios = require('axios')

const app = new Koa()

const PORT = process.env.PORT || 3000
const TODO_BACKEND_URL = process.env.TODO_BACKEND_URL || 'http://todo-backend-svc:2345/todos'
const FILES_DIR = process.env.FILES_DIR || path.join('/', 'usr', 'src', 'app', 'files')
const IMAGE_URL = process.env.IMAGE_URL || 'https://picsum.photos/1200'
const IMAGE_REFRESH_INTERVAL_MS = parseInt(process.env.IMAGE_REFRESH_INTERVAL_MS, 10) || (10 * 60 * 1000)
const MAX_TODO_LENGTH = parseInt(process.env.MAX_TODO_LENGTH, 10) || 140

app.use(bodyParser())

const imagePath = path.join(FILES_DIR, 'image.jpg')
const timestampPath = path.join(FILES_DIR, 'image_timestamp.txt')

function ensureDirectoryExists() {
  if (!fs.existsSync(FILES_DIR)) {
    fs.mkdirSync(FILES_DIR, { recursive: true })
  }
}

async function fetchAndSaveNewImage() {
  try {
    ensureDirectoryExists()
    console.log(`Fetching new image from ${IMAGE_URL}...`)
    const response = await axios.get(IMAGE_URL, { responseType: 'stream' })
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

  if (Date.now() - lastUpdated > IMAGE_REFRESH_INTERVAL_MS) {
    console.log(`Image is older than ${IMAGE_REFRESH_INTERVAL_MS / 1000}s. Triggering background refresh...`)
    fetchAndSaveNewImage()
  }

  return { lastUpdated }
}

async function fetchTodosFromBackend() {
  try {
    const response = await axios.get(TODO_BACKEND_URL)
    return response.data || []
  } catch (err) {
    console.error(`Error fetching TODOs from backend (${TODO_BACKEND_URL}):`, err.message)
    return []
  }
}

async function createTodoInBackend(text) {
  try {
    await axios.post(TODO_BACKEND_URL, { text })
    console.log(`Successfully created TODO in backend: "${text}"`)
  } catch (err) {
    console.error(`Error creating TODO in backend (${TODO_BACKEND_URL}):`, err.message)
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
    await getImageInfo()
    if (fs.existsSync(imagePath)) {
      ctx.type = 'image/jpeg'
      ctx.body = fs.createReadStream(imagePath)
    } else {
      ctx.status = 404
      ctx.body = 'Image loading...'
    }
    return
  }

  if (ctx.method === 'POST' && (ctx.path === '/todos' || ctx.path === '/')) {
    const body = ctx.request.body || {}
    const text = (body.todo || body.text || '').trim()

    if (text.length > 0 && text.length <= MAX_TODO_LENGTH) {
      await createTodoInBackend(text)
    } else if (text.length > MAX_TODO_LENGTH) {
      console.warn(`Rejected TODO: Exceeds ${MAX_TODO_LENGTH} characters (${text.length} chars)`)
    }

    ctx.redirect('/')
    return
  }

  if (ctx.path.includes('favicon.ico')) return

  const { lastUpdated } = await getImageInfo()
  const todos = await fetchTodosFromBackend()

  const todoItemsHtml = todos.length > 0
    ? todos.map(t => {
      const textStyle = t.completed ? 'line-through text-slate-500' : 'text-slate-100'
      const badgeStyle = t.completed 
        ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30' 
        : 'bg-slate-800 text-slate-400 border-slate-700'
      
      return `
      <li class="flex items-center justify-between p-3.5 bg-slate-900/60 border border-slate-700/50 rounded-xl hover:border-slate-600 transition-colors group">
        <div class="flex items-center space-x-3 flex-1 min-w-0 pr-2">
          <span class="w-2 h-2 rounded-full ${t.completed ? 'bg-emerald-400' : 'bg-sky-400'} shrink-0"></span>
          <span class="text-sm font-medium break-all ${textStyle}">${escapeHtml(t.text)}</span>
        </div>

        <div class="flex items-center space-x-2 shrink-0">
          <span class="text-[10px] font-mono px-2 py-0.5 rounded-full border ${badgeStyle}">
            ${t.completed ? 'Done' : 'Pending'}
          </span>
        </div>
      </li>
    `}).join('')
    : `<li class="text-center py-6 text-slate-400 italic text-sm border border-dashed border-slate-700/60 rounded-xl">
        No TODOs created yet. Add one above!
       </li>`

  ctx.type = 'html'
  ctx.body = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Todo Application</title>
  <link rel="stylesheet" href="/style.css">
</head>
<body class="bg-slate-900 text-slate-100 min-h-screen flex flex-col items-center justify-center p-4">
  <main class="bg-slate-800/90 border border-slate-700/60 rounded-2xl p-6 max-w-xl w-full shadow-2xl backdrop-blur-sm space-y-6">
    
    <div class="text-center">
      <h1 class="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-sky-400 to-blue-500 mb-1 tracking-tight">
        Todo Application
      </h1>
    </div>

    <div id="image-container" data-last-updated="${lastUpdated}" data-refresh-interval="${IMAGE_REFRESH_INTERVAL_MS}" class="relative rounded-xl overflow-hidden border border-slate-700/60 shadow-md bg-slate-950 flex items-center justify-center min-h-[220px]">
      <img id="main-image" src="/image.jpg" alt="Daily Image" class="w-full h-56 object-contain mx-auto">
      
      <div class="absolute bottom-2.5 right-2.5 px-3 py-1 bg-slate-900/80 backdrop-blur-md rounded-full text-[11px] font-mono font-medium text-sky-400 border border-slate-700/60 shadow-sm flex items-center gap-1.5">
        <span class="w-1.5 h-1.5 rounded-full bg-sky-400 animate-pulse"></span>
        <span id="timer-text">Next update...</span>
      </div>
    </div>

    <form action="/todos" method="POST" class="space-y-3">
      <div class="flex gap-2">
        <input 
          type="text" 
          name="todo" 
          maxlength="${MAX_TODO_LENGTH}" 
          required 
          placeholder="Enter todo (max ${MAX_TODO_LENGTH} characters)..." 
          class="flex-1 bg-slate-950 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500/50 focus:border-sky-500 transition-all"
        />
        <button 
          type="submit" 
          class="bg-gradient-to-r from-sky-500 to-blue-600 hover:from-sky-400 hover:to-blue-500 text-white font-semibold px-5 py-2.5 rounded-xl text-sm shadow-md hover:shadow-sky-500/20 active:scale-[0.98] transition-all shrink-0 cursor-pointer"
        >
          Create TODO
        </button>
      </div>
      <div class="flex justify-end items-center text-[11px] text-slate-400 px-1">
        <span>Max ${MAX_TODO_LENGTH} characters</span>
      </div>
    </form>

    <div class="space-y-2">
      <h2 class="text-xs font-semibold uppercase tracking-wider text-slate-400 px-1">Your TODOs</h2>
      <ul class="space-y-2 max-h-64 overflow-y-auto pr-1 scrollbar-thin scrollbar-thumb-slate-700">
        ${todoItemsHtml}
      </ul>
    </div>

  </main>

  <script>
    (function() {
      const container = document.getElementById('image-container');
      const timerText = document.getElementById('timer-text');
      const img = document.getElementById('main-image');
      const lastUpdated = parseInt(container.getAttribute('data-last-updated') || '0', 10);
      const refreshIntervalMs = parseInt(container.getAttribute('data-refresh-interval') || '600000', 10);

      function updateTimer() {
        if (!lastUpdated) {
          timerText.textContent = 'Next update...';
          return;
        }

        const now = Date.now();
        const elapsed = now - lastUpdated;
        const remaining = Math.max(0, refreshIntervalMs - elapsed);

        if (remaining <= 0) {
          timerText.textContent = 'Updating image...';
          setTimeout(() => {
            img.src = '/image.jpg?' + Date.now();
          }, 3000);
          return;
        }

        const minutes = Math.floor(remaining / 60000);
        const seconds = Math.floor((remaining % 60000) / 1000);
        const formattedSeconds = seconds < 10 ? '0' + seconds : seconds;
        timerText.textContent = 'Next update in ' + minutes + ':' + formattedSeconds;
      }

      setInterval(updateTimer, 1000);
      updateTimer();
    })();
  </script>
</body>
</html>`
})

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;')
}

console.log(`Todo App server starting on port ${PORT}. Configuration: TODO_BACKEND_URL=${TODO_BACKEND_URL}, FILES_DIR=${FILES_DIR}, IMAGE_URL=${IMAGE_URL}, REFRESH_INTERVAL=${IMAGE_REFRESH_INTERVAL_MS}ms, MAX_TODO_LENGTH=${MAX_TODO_LENGTH}`)
app.listen(PORT)
