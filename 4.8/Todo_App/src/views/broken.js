module.exports = function renderBroken() {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Application Broken</title>
  <link rel="stylesheet" href="/style.css">
  <meta http-equiv="refresh" content="5">
</head>
<body class="bg-slate-900 text-slate-100 min-h-screen flex flex-col items-center justify-center p-4">
  <main class="bg-red-950/80 border border-red-700/60 rounded-2xl p-8 max-w-md w-full shadow-2xl text-center space-y-4">
    <div class="inline-flex p-3 rounded-full bg-red-500/20 text-red-400 border border-red-500/30 mb-2">
      <svg xmlns="http://www.w3.org/2000/svg" class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77-1.333.192-3 1.732-3z" />
      </svg>
    </div>
    <h1 class="text-2xl font-bold text-red-300">Application Broken!</h1>
    <p class="text-sm text-red-200">The health probe is now failing with HTTP 500 status code.</p>
    <p class="text-xs text-slate-400">Kubernetes liveness probe will detect this failure shortly and restart the pod automatically.</p>
    <div class="pt-2">
      <a href="/" class="inline-block bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs px-4 py-2 rounded-xl transition-colors">Check Status</a>
    </div>
  </main>
</body>
</html>`
}
