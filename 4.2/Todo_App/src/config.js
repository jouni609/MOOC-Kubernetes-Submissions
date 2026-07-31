const path = require('path')

function parseInteger(name, fallback) {
  const value = Number.parseInt(process.env[name], 10)
  return Number.isNaN(value) ? fallback : value
}

module.exports = {
  port: parseInteger('PORT', 3000),
  todoBackendUrl: process.env.TODO_BACKEND_URL || 'http://todo-backend-svc:2345/todos',
  filesDir: process.env.FILES_DIR || path.join('/', 'usr', 'src', 'app', 'files'),
  imageUrl: process.env.IMAGE_URL || 'https://picsum.photos/1200',
  imageRefreshIntervalMs: parseInteger('IMAGE_REFRESH_INTERVAL_MS', 10 * 60 * 1000),
  maxTodoLength: parseInteger('MAX_TODO_LENGTH', 140)
}
