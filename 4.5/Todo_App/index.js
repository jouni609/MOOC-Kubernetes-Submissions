const createApp = require('./src/app')
const config = require('./src/config')

const app = createApp()

console.log(`Todo App server starting on port ${config.port}. Configuration: TODO_BACKEND_URL=${config.todoBackendUrl}, FILES_DIR=${config.filesDir}, IMAGE_URL=${config.imageUrl}, REFRESH_INTERVAL=${config.imageRefreshIntervalMs}ms, MAX_TODO_LENGTH=${config.maxTodoLength}`)
app.listen(config.port, '0.0.0.0')
