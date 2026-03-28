const { processExecutionUpdate } = require('../services/webhook-service');

async function handleWebhookSimulation({ req, res, body, backendBaseUrl, fetchImpl }) {
  const payload = body ? JSON.parse(body) : {};
  const result = await processExecutionUpdate({
    payload,
    backendBaseUrl,
    fetchImpl,
  });

  res.writeHead(result.statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(result.body));
}

module.exports = {
  handleWebhookSimulation,
};
