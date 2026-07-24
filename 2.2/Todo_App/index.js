import Koa from 'koa'
import { bodyParser } from '@koa/bodyparser'
import { existsSync, mkdirSync, createWriteStream, renameSync, writeFileSync, readFileSync, statSync, createReadStream } from 'fs'
import { join } from 'path'
import { get, post } from 'axios'

const app = new Koa()
const PORT = process.env.PORT || 3000
const TODO_BACKEND_URL = process.env.TODO_BACKEND_URL || 'http://todo-backend-svc:2345/todos'

app.use(bodyParser())

const directory = join('/', 'usr', 'src', 'app', 'files')
const imagePath = join(directory, 'image.jpg')
const timestampPath = join(directory, 'image_timestamp.txt')

const TEN_MINUTES_MS = 10 * 60 * 1000

function ensureDirectoryExists() {
  if (!existsSync(directory)) {
    mkdirSync(directory, { recursive: true })
  }
}

async function fetchAndSaveNewImage() {
  try {
    ensureDirectoryExists()
    console.log('Fetching new image from Lorem Picsum...')
    const response = await get('https://picsum.photos/1200', { responseType: 'stream' })
    const tempPath = `${imagePath}.tmp`
    const writer = createWriteStream(tempPath)
    
    response.data.pipe(writer)
    
    await new Promise((resolve, reject) => {
      writer.on('finish', resolve)
      writer.on('error', reject)
    })
    
    renameSync(tempPath, imagePath)
    const now = Date.now()
    writeFileSync(timestampPath, now.toString())
    console.log(`Successfully updated image.jpg on persistent volume at ${new Date(now).toISOString()}.`)
    return now
  } catch (err) {
    console.error('Failed to fetch new image:', err.message)
    return Date.now()
  }
}

async function getImageInfo() {
  ensureDirectoryExists()
  const exists = existsSync(imagePath)
  
  if (!exists) {
    const newTimestamp = await fetchAndSaveNewImage()
    return { lastUpdated: newTimestamp }
  }

  let lastUpdated = 0
  if (existsSync(timestampPath)) {
    lastUpdated = parseInt(readFileSync(timestampPath, 'utf-8'), 10) || 0
  } else {
    const stats = statSync(imagePath)
    lastUpdated = Math.floor(stats.mtimeMs)
  }

  if (Date.now() - lastUpdated > TEN_MINUTES_MS) {
    console.log('Image is older than 10 minutes. Triggering background refresh...')
    fetchAndSaveNewImage()
  }

  return { lastUpdated }
}

async function fetchTodosFromBackend() {
  try {
    const response = await get(TODO_BACKEND_URL)
    return response.data || []
  } catch (err) {
    console.error(`Error fetching TODOs from backend (${TODO_BACKEND_URL}):`, err.message)
    return []
  }
}

async function createTodoInBackend(text) {
  try {
    await post(TODO_BACKEND_URL, { text })
    console.log(`Successfully created TODO in backend: "${text}"`)
  } catch (err) {
    console.error(`Error creating TODO in backend (${TODO_BACKEND_URL}):`, err.message)
  }
}

app.use(async ctx => {
  // Static CSS file route
  if (ctx.path === '/style.css') {
    ctx.type = 'text/css'
    const cssPath = join(__dirname, 'public', 'dist.css')
    if (existsSync(cssPath)) {
      ctx.body = createReadStream(cssPath)
    } else {
      ctx.body = createReadStream(join(__dirname, 'public', 'style.css'))
    }
    return
  }

  // Image route
  if (ctx.path === '/image.jpg') {
    await getImageInfo()
    if (existsSync(imagePath)) {
      ctx.type = 'image/jpeg'
      ctx.body = createReadStream(imagePath)
    } else {
      ctx.status = 404
      ctx.body = 'Image loading...'
    }
    return
  }

  // Create new TODO endpoint (POST /todos or POST /)
  if (ctx.method === 'POST' && (ctx.path === '/todos' || ctx.path === '/')) {
    const body = ctx.request.body || {}
    const text = (body.todo || body.text || '').trim()

    if (text.length > 0 && text.length <= 140) {
      await createTodoInBackend(text)
    } else if (text.length > 140) {
      console.warn(`Rejected TODO: Exceeds 140 characters (${text.length} chars)`)
    }

    ctx.redirect('/')
    return
  }

  if (ctx.path.includes('favicon.ico')) return

  // Main UI route (GET /)
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
    
    <!-- Header -->
    <div class="text-center">
      <h1 class="text-3xl font-extrabold text-transparent bg-clip-text bg-linear-to-r from-sky-400 to-blue-500 mb-1 tracking-tight">
        Todo Application
      </h1>
    </div>

    <!-- Cached Image Display -->
    <div id="image-container" data-last-updated="${lastUpdated}" class="relative rounded-xl overflow-hidden border border-slate-700/60 shadow-md bg-slate-950 flex items-center justify-center min-h-55">
      <img id="main-image" src="/image.jpg" alt="Daily Image" class="w-full h-56 object-contain mx-auto">
      
      <!-- Timer Badge in Bottom Right Corner -->
      <div class="absolute bottom-2.5 right-2.5 px-3 py-1 bg-slate-900/80 backdrop-blur-md rounded-full text-[11px] font-mono font-medium text-sky-400 border border-slate-700/60 shadow-sm flex items-center gap-1.5">
        <span class="w-1.5 h-1.5 rounded-full bg-sky-400 animate-pulse"></span>
        <span id="timer-text">Next update in 10:00</span>
      </div>
    </div>

    <!-- Create Todo Form -->
    <form action="/todos" method="POST" class="space-y-3">
      <div class="flex gap-2">
        <input 
          type="text" 
          name="todo" 
          maxlength="140" 
          required 
          placeholder="Enter todo (max 140 characters)..." 
          class="flex-1 bg-slate-950 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500/50 focus:border-sky-500 transition-all"
        />
        <button 
          type="submit" 
          class="bg-linear-to-r from-sky-500 to-blue-600 hover:from-sky-400 hover:to-blue-500 text-white font-semibold px-5 py-2.5 rounded-xl text-sm shadow-md hover:shadow-sky-500/20 active:scale-[0.98] transition-all shrink-0 cursor-pointer"
        >
          Create TODO
        </button>
      </div>
      <div class="flex justify-end items-center text-[11px] text-slate-400 px-1">
        <span>Max 140 characters</span>
      </div>
    </form>

    <!-- Todo List -->
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
      const tenMinutesMs = 10 * 60 * 1000;

      function updateTimer() {
        if (!lastUpdated) {
          timerText.textContent = 'Next update in 10:00';
          return;
        }

        const now = Date.now();
        const elapsed = now - lastUpdated;
        const remaining = Math.max(0, tenMinutesMs - elapsed);

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

console.log(`Todo App server starting on port ${PORT}. Target TODO_BACKEND_URL: ${TODO_BACKEND_URL}...`)
app.listen(PORT)
