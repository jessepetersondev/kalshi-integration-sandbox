const config = {
  port: Number.parseInt(process.env.PORT ?? '3001', 10),
  backendBaseUrl: process.env.BACKEND_BASE_URL ?? 'http://localhost:5145',
};

module.exports = { config };
