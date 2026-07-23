const Koa = require('koa')
const { bodyParser } = require('@koa/bodyparser')
const fs = require('fs')
const path = require('path')
const axios = require('axios')

const app = new Koa()
const PORT = process.env.PORT || 3000

app.use(bodyParser())

const directory = path.join('/', 'usr', 'src', 'app', 'files')
const imagePath = path.join(directory, 'image.jpg')
const timestampPath = path.join(directory, 'image_timestamp.txt')
const todosPath = path.join(directory, 'todos.json')

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

  if (Date.now() - lastUpdated > TEN_MINUTES_MS) {
    console.log('Image is older than 10 minutes. Triggering background refresh...')
    fetchAndSaveNewImage()
  }

  return { lastUpdated }
}

function getTodos() {
  ensureDirectoryExists()
  if (fs.existsSync(todosPath)) {
    try {
      const data = fs.readFileSync(todosPath, 'utf-8')
      const parsed = JSON.parse(data)
      return parsed.map(t => ({
        id: t.id || Date.now().toString(),
        text: t.text || '',
        completed: Boolean(t.completed),
        createdAt: t.createdAt || new Date().toISOString()
      }))
    } catch (err) {
      console.error('Error reading todos.json:', err.message)
      return []
    }
  }
  return []
}

function saveTodos(todos) {
  ensureDirectoryExists()
  fs.writeFileSync(todosPath, JSON.stringify(todos, null, 2), 'utf-8')
}

app.use(async ctx => {
  // Static CSS file route
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

  // Image route
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

  // Toggle todo completion endpoint
  if (ctx.method === 'POST' && ctx.path.startsWith('/todos/toggle')) {
    const body = ctx.request.body || {}
    const id = body.id || ctx.path.split('/')[3]
    if (id) {
      const todos = getTodos()
      const todo = todos.find(t => t.id === id)
      if (todo) {
        todo.completed = !todo.completed
        saveTodos(todos)
        console.log(`Toggled completion for TODO "${todo.text}": completed = ${todo.completed}`)
      }
    }
    ctx.redirect('/')
    return
  }

  // Delete todo endpoint
  if (ctx.method === 'POST' && ctx.path.startsWith('/todos/delete')) {
    const body = ctx.request.body || {}
    const id = body.id || ctx.path.split('/')[3]
    if (id) {
      let todos = getTodos()
      const initialCount = todos.length
      todos = todos.filter(t => t.id !== id)
      if (todos.length < initialCount) {
        saveTodos(todos)
        console.log(`Deleted TODO with id ${id}`)
      }
    }
    ctx.redirect('/')
    return
  }

  // Create new TODO endpoint (POST /todos or POST /)
  if (ctx.method === 'POST' && (ctx.path === '/todos' || ctx.path === '/')) {
    const body = ctx.request.body || {}
    const text = (body.todo || body.text || '').trim()

    if (text.length > 0 && text.length <= 140) {
      const todos = getTodos()
      todos.push({
        id: Date.now().toString(),
        text: text,
        completed: false,
        createdAt: new Date().toISOString()
      })
      saveTodos(todos)
      console.log(`Created new TODO (${text.length} chars): "${text}"`)
    } else if (text.length > 140) {
      console.warn(`Rejected TODO: Exceeds 140 characters (${text.length} chars)`)
    }

    ctx.redirect('/')
    return
  }

  if (ctx.path.includes('favicon.ico')) return

  // Main UI route (GET /)
  const { lastUpdated } = await getImageInfo()
  const todos = getTodos()

  const todoItemsHtml = todos.length > 0
    ? todos.map(t => {
      const textStyle = t.completed ? 'line-through text-slate-500' : 'text-slate-100'
      const badgeStyle = t.completed 
        ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30' 
        : 'bg-slate-800 text-slate-400 border-slate-700'
      
      return `
      <li class="flex items-center justify-between p-3.5 bg-slate-900/60 border border-slate-700/50 rounded-xl hover:border-slate-600 transition-colors group">
        <div class="flex items-center space-x-3 flex-1 min-w-0 pr-2">
          <!-- Toggle Checkbox Form -->
          <form action="/todos/toggle" method="POST" class="inline flex items-center">
            <input type="hidden" name="id" value="${t.id}" />
            <button type="submit" class="w-5 h-5 rounded-md border ${t.completed ? 'bg-emerald-500 border-emerald-500 text-slate-950' : 'border-slate-600 hover:border-sky-400'} flex items-center justify-center transition-all cursor-pointer">
              ${t.completed ? '<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"></path></svg>' : ''}
            </button>
          </form>
          
          <span class="text-sm font-medium break-all ${textStyle}">${escapeHtml(t.text)}</span>
        </div>

        <div class="flex items-center space-x-2 shrink-0">
          <span class="text-[10px] font-mono px-2 py-0.5 rounded-full border ${badgeStyle}">
            ${t.completed ? 'Done' : 'Pending'}
          </span>

          <!-- Delete Form -->
          <form action="/todos/delete" method="POST" class="inline">
            <input type="hidden" name="id" value="${t.id}" />
            <button type="submit" title="Delete TODO" class="p-1 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition-colors cursor-pointer">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
              </svg>
            </button>
          </form>
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
      <h1 class="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-sky-400 to-blue-500 mb-1 tracking-tight">
        Todo Application
      </h1>
    </div>

    <!-- Cached Image Display with Live Countdown Timer in Bottom Right -->
    <div id="image-container" data-last-updated="${lastUpdated}" class="relative rounded-xl overflow-hidden border border-slate-700/60 shadow-md bg-slate-950 flex items-center justify-center min-h-[220px]">
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
          class="bg-gradient-to-r from-sky-500 to-blue-600 hover:from-sky-400 hover:to-blue-500 text-white font-semibold px-5 py-2.5 rounded-xl text-sm shadow-md hover:shadow-sky-500/20 active:scale-[0.98] transition-all shrink-0 cursor-pointer"
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

console.log(`Todo App server starting on port ${PORT}...`)
app.listen(PORT)
