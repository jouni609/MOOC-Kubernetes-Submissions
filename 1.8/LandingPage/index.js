const Koa = require('koa')
const fs = require('fs')
const path = require('path')

const app = new Koa()
const PORT = process.env.PORT || 3000

// In-memory log buffer for live console output
const logsBuffer = []
const MAX_LOGS = 100

function addLog(message) {
  const timestamp = new Date().toISOString()
  const entry = `${timestamp}: ${message}`
  console.log(entry)
  logsBuffer.push(entry)
  if (logsBuffer.length > MAX_LOGS) {
    logsBuffer.shift()
  }
}

// Generate periodic container activity log
const instanceId = Math.random().toString(36).substring(2, 8)
addLog(`Application initialized [Instance ID: ${instanceId}]`)

setInterval(() => {
  const randomHash = Math.random().toString(36).substring(2, 10)
  addLog(`Ping status OK - Heartbeat hash: ${randomHash}`)
}, 5000)

// Helper to serve CSS stylesheet
function serveCss(ctx) {
  ctx.type = 'text/css'
  const distPath = path.join(__dirname, 'public', 'dist.css')
  const srcPath = path.join(__dirname, 'public', 'style.css')
  const targetPath = fs.existsSync(distPath) ? distPath : srcPath
  ctx.body = fs.createReadStream(targetPath)
}

// Helper to render HTML page
function renderHtml(port, instance) {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Landing Page - Exercise 1.8</title>
  <link rel="stylesheet" href="/style.css">
</head>
<body class="bg-slate-900 text-slate-100 min-h-screen flex items-center justify-center p-4">
  <div class="bg-slate-800/90 border border-slate-700/60 rounded-2xl p-6 max-w-2xl w-full shadow-2xl backdrop-blur-sm space-y-6">
    
    <!-- Header -->
    <div class="text-center">
      <h1 class="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-sky-400 to-blue-500 mb-2 tracking-tight">
        Exercise 1.8: Project Ingress Access
      </h1>
      <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold bg-sky-500/10 text-sky-400 border border-sky-500/20 shadow-sm">
        <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
        Container Port: ${port} | Route: Ingress (8081) | Instance: ${instance}
      </div>
    </div>

    <!-- Live Terminal / Console View -->
    <div class="bg-slate-950 border border-slate-800 rounded-xl overflow-hidden shadow-inner">
      <!-- Window Title Bar -->
      <div class="bg-slate-900/80 px-4 py-2.5 border-b border-slate-800 flex items-center justify-end">
        <div class="flex items-center space-x-1.5 text-xs text-emerald-400 font-mono">
          <span class="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span>
          <span>LIVE</span>
        </div>
      </div>
      
      <!-- Console Output Body -->
      <div id="console-output" class="p-4 h-64 overflow-y-auto font-mono text-xs text-emerald-400 space-y-1 scrollbar-thin scrollbar-thumb-slate-700">
        <div class="text-slate-500 italic">Connecting to live console stream...</div>
      </div>
    </div>

  </div>

  <script>
    const consoleOutput = document.getElementById('console-output');
    let autoScroll = true;

    consoleOutput.addEventListener('scroll', () => {
      const distanceToBottom = consoleOutput.scrollHeight - consoleOutput.clientHeight - consoleOutput.scrollTop;
      autoScroll = distanceToBottom < 30;
    });

    async function fetchLogs() {
      try {
        const res = await fetch('/api/logs');
        if (!res.ok) return;
        const logs = await res.json();
        
        consoleOutput.innerHTML = logs.map(line => {
          return '<div class="leading-relaxed hover:bg-slate-900/50 rounded px-1 -mx-1 transition-colors">' + 
            escapeHtml(line) + 
          '</div>';
        }).join('');

        if (autoScroll) {
          consoleOutput.scrollTop = consoleOutput.scrollHeight;
        }
      } catch (err) {
        console.error('Failed to fetch logs:', err);
      }
    }

    function escapeHtml(str) {
      return str.replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
    }

    fetchLogs();
    setInterval(fetchLogs, 2000);
  </script>
</body>
</html>`
}

app.use(async ctx => {
  if (ctx.path === '/style.css') {
    return serveCss(ctx)
  }

  if (ctx.path === '/api/logs') {
    ctx.type = 'application/json'
    ctx.body = JSON.stringify(logsBuffer)
    return
  }

  if (ctx.path.includes('favicon.ico')) return

  addLog(`GET / request received`)
  ctx.type = 'html'
  ctx.body = renderHtml(PORT, instanceId)
})

console.log(`Server starting on port ${PORT}`)
app.listen(PORT)
