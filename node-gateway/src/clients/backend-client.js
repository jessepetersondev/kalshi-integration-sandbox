async function forwardExecutionUpdate({ backendBaseUrl, payload, fetchImpl = fetch }) {
  const response = await fetchImpl(`${backendBaseUrl}/api/v1/integrations/execution-updates`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'x-correlation-id': payload.correlationId ?? payload.orderId,
    },
    body: JSON.stringify(payload),
  });

  const raw = await response.text();
  let body = null;

  if (raw) {
    try {
      body = JSON.parse(raw);
    } catch {
      body = { raw };
    }
  }

  if (!response.ok) {
    const error = new Error(`Backend forwarding failed with status ${response.status}.`);
    error.status = response.status;
    error.body = body;
    throw error;
  }

  return body;
}

module.exports = {
  forwardExecutionUpdate,
};
