const allowedStatuses = new Set([
  'pending',
  'accepted',
  'resting',
  'partially_filled',
  'filled',
  'canceled',
  'rejected',
  'settled',
]);

function validateExecutionUpdate(payload) {
  const errors = [];

  if (!payload || typeof payload !== 'object') {
    return ['Payload must be a JSON object.'];
  }

  if (!payload.orderId || typeof payload.orderId !== 'string') {
    errors.push('orderId is required and must be a string.');
  }

  if (!payload.status || typeof payload.status !== 'string' || !allowedStatuses.has(payload.status)) {
    errors.push(`status is required and must be one of: ${Array.from(allowedStatuses).join(', ')}.`);
  }

  if (payload.filledQuantity !== undefined && (!Number.isFinite(payload.filledQuantity) || payload.filledQuantity < 0)) {
    errors.push('filledQuantity must be a non-negative number when provided.');
  }

  if (payload.occurredAt !== undefined && Number.isNaN(Date.parse(payload.occurredAt))) {
    errors.push('occurredAt must be a valid ISO-8601 date string when provided.');
  }

  return errors;
}

module.exports = {
  allowedStatuses,
  validateExecutionUpdate,
};
