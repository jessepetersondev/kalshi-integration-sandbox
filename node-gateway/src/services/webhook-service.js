const { validateExecutionUpdate } = require('../models/execution-update');
const { forwardExecutionUpdate } = require('../clients/backend-client');

async function processExecutionUpdate({ payload, backendBaseUrl, fetchImpl }) {
  const errors = validateExecutionUpdate(payload);

  if (errors.length > 0) {
    return {
      ok: false,
      statusCode: 400,
      body: {
        type: 'validation_error',
        message: 'Execution update payload is invalid.',
        errors,
      },
    };
  }

  const normalizedPayload = {
    ...payload,
    occurredAt: payload.occurredAt ?? new Date().toISOString(),
  };

  try {
    const forwarded = await forwardExecutionUpdate({
      backendBaseUrl,
      payload: normalizedPayload,
      fetchImpl,
    });

    return {
      ok: true,
      statusCode: 202,
      body: {
        status: 'accepted',
        forwarded,
      },
    };
  } catch (error) {
    return {
      ok: false,
      statusCode: error.status ?? 502,
      body: {
        type: 'forwarding_error',
        message: error.message,
        details: error.body ?? null,
      },
    };
  }
}

module.exports = {
  processExecutionUpdate,
};
