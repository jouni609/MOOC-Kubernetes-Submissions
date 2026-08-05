function registerTodoRoutes(router, { backendClient, maxTodoLength }) {
  router.put('/todos/:id', async ctx => {
    const body = ctx.request.body || {}
    const done = body.done ?? body.completed

    if (typeof done !== 'boolean') {
      ctx.status = 400
      ctx.body = { error: 'Request must include a boolean done field.' }
      return
    }

    try {
      await backendClient.updateTodo(ctx.params.id, done)
      ctx.status = 204
    } catch (err) {
      ctx.status = err.response?.status || 502
      ctx.body = err.response?.data || { error: 'Unable to update TODO.' }
    }
  })

  router.post(['/todos', '/'], async ctx => {
    const body = ctx.request.body || {}
    const text = (body.todo || body.text || '').trim()

    if (text.length > 0 && text.length <= maxTodoLength) {
      await backendClient.createTodo(text)
    } else if (text.length > maxTodoLength) {
      console.warn(`Rejected TODO: Exceeds ${maxTodoLength} characters (${text.length} chars)`)
    }

    ctx.redirect('/')
  })
}

module.exports = registerTodoRoutes
