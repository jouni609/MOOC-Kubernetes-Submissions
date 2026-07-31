const escapeHtml = require('../utils/escape-html')

function renderTodoItems(todos) {
  if (todos.length === 0) {
    return `<li class="text-center py-6 text-slate-400 italic text-sm border border-dashed border-slate-700/60 rounded-xl">
        No TODOs created yet. Add one above!
       </li>`
  }

  return todos.map(todo => {
    const textStyle = todo.completed ? 'line-through text-slate-500' : 'text-slate-100'
    const badgeStyle = todo.completed
      ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
      : 'bg-slate-800 text-slate-400 border-slate-700'

    return `
      <li class="flex items-center justify-between p-3.5 bg-slate-900/60 border border-slate-700/50 rounded-xl hover:border-slate-600 transition-colors group">
        <div class="flex items-center space-x-3 flex-1 min-w-0 pr-2">
          <span class="w-2 h-2 rounded-full ${todo.completed ? 'bg-emerald-400' : 'bg-sky-400'} shrink-0"></span>
          <span class="text-sm font-medium break-all ${textStyle}">${escapeHtml(todo.text)}</span>
        </div>

        <div class="flex items-center space-x-2 shrink-0">
          <span class="text-[10px] font-mono px-2 py-0.5 rounded-full border ${badgeStyle}">
            ${todo.completed ? 'Done' : 'Pending'}
          </span>
        </div>
      </li>
    `
  }).join('')
}

module.exports = function renderHome({ lastUpdated, refreshIntervalMs, maxTodoLength, todos, isHealthy }) {
  const todoItemsHtml = renderTodoItems(todos)

  return `<!DOCTYPE html>
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

    <div id="image-container" data-last-updated="${lastUpdated}" data-refresh-interval="${refreshIntervalMs}" class="relative rounded-xl overflow-hidden border border-slate-700/60 shadow-md bg-slate-950 flex items-center justify-center min-h-[220px]">
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
          maxlength="${maxTodoLength}"
          required
          placeholder="Enter todo (max ${maxTodoLength} characters)..."
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
        <span>Max ${maxTodoLength} characters</span>
      </div>
    </form>

    <div class="space-y-2">
      <h2 class="text-xs font-semibold uppercase tracking-wider text-slate-400 px-1">Your TODOs</h2>
      <ul class="space-y-2 max-h-64 overflow-y-auto pr-1 scrollbar-thin scrollbar-thumb-slate-700">
        ${todoItemsHtml}
      </ul>
    </div>

    <form action="/break" method="POST" class="pt-4 border-t border-slate-700/60 flex justify-between items-center">
      <span class="text-xs text-slate-400">Application Status: <span class="${isHealthy ? 'text-emerald-400 font-semibold' : 'text-red-400 font-semibold'}">${isHealthy ? 'Healthy' : 'Unhealthy'}</span></span>
      <button
        type="submit"
        class="bg-gradient-to-r from-red-600 to-rose-700 hover:from-red-500 hover:to-rose-600 text-white font-semibold px-4 py-2 rounded-xl text-xs shadow-md hover:shadow-red-500/20 active:scale-[0.98] transition-all cursor-pointer flex items-center gap-1.5"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77-1.333.192-3 1.732-3z" />
        </svg>
        Break Application
      </button>
    </form>

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
}
