const test = require('node:test');
const assert = require('node:assert/strict');
const { processExecutionUpdate } = require('../src/services/webhook-service');

test('rejects invalid payloads with structured validation errors', async () => {
  const result = await processExecutionUpdate({
    payload: { orderId: '', status: 'bogus' },
    backendBaseUrl: 'http://localhost:5145',
    fetchImpl: async () => {
      throw new Error('should not be called');
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.statusCode, 400);
  assert.equal(result.body.type, 'validation_error');
  assert.ok(result.body.errors.length >= 2);
});

test('forwards valid payloads to backend and returns accepted', async () => {
  const calls = [];
  const fakeFetch = async (url, options) => {
    calls.push({ url, options });
    return {
      ok: true,
      status: 202,
      text: async () => JSON.stringify({ accepted: true }),
    };
  };

  const result = await processExecutionUpdate({
    payload: {
      orderId: 'ord-123',
      status: 'filled',
      filledQuantity: 2,
      correlationId: 'corr-123',
    },
    backendBaseUrl: 'http://localhost:5145',
    fetchImpl: fakeFetch,
  });

  assert.equal(result.ok, true);
  assert.equal(result.statusCode, 202);
  assert.equal(calls.length, 1);
  assert.match(calls[0].url, /\/api\/v1\/integrations\/execution-updates$/);
});

test('returns forwarding_error when backend call fails', async () => {
  const fakeFetch = async () => ({
    ok: false,
    status: 500,
    text: async () => JSON.stringify({ message: 'backend blew up' }),
  });

  const result = await processExecutionUpdate({
    payload: {
      orderId: 'ord-456',
      status: 'accepted',
    },
    backendBaseUrl: 'http://localhost:5145',
    fetchImpl: fakeFetch,
  });

  assert.equal(result.ok, false);
  assert.equal(result.statusCode, 500);
  assert.equal(result.body.type, 'forwarding_error');
});
