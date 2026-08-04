const renderBroken = require('../views/broken')

function registerBreakRoutes(router, { healthState }) {
  router.post(['/break', '/todos/break'], ctx => {
    console.warn('[WARN] Break application triggered by user! Setting app health to false.')
    healthState.breakApplication()
    ctx.type = 'html'
    ctx.body = renderBroken()
  })
}

module.exports = registerBreakRoutes
