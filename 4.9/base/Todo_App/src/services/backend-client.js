const axios = require('axios')

function createBackendClient(todoBackendUrl) {
  const backendBaseUrl = todoBackendUrl.replace(/\/todos\/?$/, '')

  async function fetchTodos() {
    try {
      const response = await axios.get(todoBackendUrl)
      return response.data || []
    } catch (err) {
      console.error(`Error fetching TODOs from backend (${todoBackendUrl}):`, err.message)
      return []
    }
  }

  async function createTodo(text) {
    try {
      await axios.post(todoBackendUrl, { text })
      console.log(`Successfully created TODO in backend: "${text}"`)
    } catch (err) {
      console.error(`Error creating TODO in backend (${todoBackendUrl}):`, err.message)
    }
  }

  async function updateTodo(id, done) {
    try {
      const response = await axios.put(`${todoBackendUrl}/${encodeURIComponent(id)}`, { done })
      console.log(`Successfully updated TODO ${id}: done=${done}`)
      return response.data
    } catch (err) {
      console.error(`Error updating TODO ${id} in backend (${todoBackendUrl}):`, err.message)
      throw err
    }
  }

  async function checkHealth() {
    try {
      const response = await axios.get(`${backendBaseUrl}/healthprobe`, { timeout: 3000 })
      return response.status === 200
    } catch (err) {
      try {
        const response = await axios.get(todoBackendUrl, { timeout: 3000 })
        return response.status === 200
      } catch (fallbackError) {
        return false
      }
    }
  }

  return {
    fetchTodos,
    createTodo,
    updateTodo,
    checkHealth
  }
}

module.exports = createBackendClient
