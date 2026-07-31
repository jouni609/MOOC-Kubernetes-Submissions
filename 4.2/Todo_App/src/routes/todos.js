function registerTodoRoutes(router, { backendClient, maxTodoLength }) {
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
