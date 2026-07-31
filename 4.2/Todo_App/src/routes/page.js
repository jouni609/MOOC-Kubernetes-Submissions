const renderHome = require('../views/home')

function registerPageRoutes(router, { backendClient, imageService, healthState, config }) {
  router.get('/', async ctx => {
    const { lastUpdated } = await imageService.getImageInfo()
    const todos = await backendClient.fetchTodos()

    ctx.type = 'html'
    ctx.body = renderHome({
      lastUpdated,
      refreshIntervalMs: config.imageRefreshIntervalMs,
      maxTodoLength: config.maxTodoLength,
      todos,
      isHealthy: healthState.isHealthy()
    })
  })
}

module.exports = registerPageRoutes
