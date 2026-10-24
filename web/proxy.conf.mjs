// Proxy do ng serve: o backend não habilita CORS, então o navegador fala só com o dev-server
// e as chamadas /api são repassadas para a API .NET.
const target = process.env.ANTECIPE_API_URL ?? 'http://localhost:5000';

export default {
  '/api': { target, secure: false, changeOrigin: true, logLevel: 'warn' },
};
