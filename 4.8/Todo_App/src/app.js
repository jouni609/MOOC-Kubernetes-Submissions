const Koa = require('koa')
const Router = require('@koa/router')
const { bodyParser } = require('@koa/bodyparser')
const path = require('path')

const defaultConfig = require('./config')
const defaultHealthState = require('./health-state')
const createBackendClient = require('./services/backend-client')
const createImageService = require('./services/image-service')
const registerAssetRoutes = require('./routes/assets')
const registerBreakRoutes = require('./routes/break')
const registerHealthRoutes = require('./routes/health')
const registerPageRoutes = require('./routes/page')
const registerTodoRoutes = require('./routes/todos')

function createApp({ config = defaultConfig, healthState = defaultHealthState, backendClient, imageService } = {}) {
  const app = new Koa()
  const router = new Router()
  const resolvedBackendClient = backendClient || createBackendClient(config.todoBackendUrl)
  const resolvedImageService = imageService || createImageService(config)

  app.use(bodyParser())

  registerHealthRoutes(router, {
    healthState,
    backendClient: resolvedBackendClient
  })
  registerBreakRoutes(router, { healthState })
  registerAssetRoutes(router, {
    imageService: resolvedImageService,
    publicDir: path.join(__dirname, '..', 'public')
  })
  registerTodoRoutes(router, {
    backendClient: resolvedBackendClient,
    maxTodoLength: config.maxTodoLength
  })
  registerPageRoutes(router, {
    backendClient: resolvedBackendClient,
    imageService: resolvedImageService,
    healthState,
    config
  })

  app.use(router.routes())
  app.use(router.allowedMethods())

  return app
}

module.exports = createApp
