function registerHealthRoutes(router, { healthState, backendClient }) {
  router.get('/health', ctx => {
    if (!healthState.isHealthy()) {
      ctx.status = 500
      ctx.body = { status: 'unhealthy', reason: 'app broken by user' }
      return
    }

    ctx.status = 200
    ctx.body = { status: 'ok' }
  })

  router.get('/healthprobe', async ctx => {
    if (!healthState.isHealthy()) {
      console.warn('[WARN] [PROBE] GET /healthprobe called while app is unhealthy -> returning 500')
      ctx.status = 500
      ctx.body = { status: 'unhealthy', reason: 'app broken by user' }
      return
    }

    const backendStatus = await backendClient.checkHealth()
    if (!backendStatus) {
      console.warn('[WARN] [PROBE] GET /healthprobe -> backend check failed -> returning 500 unhealthy')
      ctx.status = 500
      ctx.body = { status: 'unhealthy', reason: 'backend unreachable' }
      return
    }

    ctx.status = 200
    ctx.body = { status: 'ok' }
  })
}

module.exports = registerHealthRoutes
