// Em produção, o front é servido atrás do mesmo host da API (reverse proxy). Ajuste se a API
// estiver em outra origem — nesse caso o backend precisará habilitar CORS.
export const environment = {
  production: true,
  apiBaseUrl: '',
};
