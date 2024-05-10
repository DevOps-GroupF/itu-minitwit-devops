const { defineConfig } = require("cypress");

module.exports = defineConfig({
  e2e: {
    baseUrl: 'http://minitwit-service:8080/',
    supportFile: false,
    experimentalRunAllSpecs: true
  },
})
