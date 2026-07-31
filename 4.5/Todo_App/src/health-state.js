let healthy = true

function isHealthy() {
  return healthy
}

function breakApplication() {
  healthy = false
}

module.exports = {
  isHealthy,
  breakApplication
}
