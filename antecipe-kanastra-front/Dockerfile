# Build do Angular e entrega estática pelo nginx, que também repassa /api para o backend.
FROM node:24-alpine AS build
WORKDIR /app
ENV NG_CLI_ANALYTICS=false
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npx ng build --configuration production

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/antecipe-kanastra-front/browser /usr/share/nginx/html
EXPOSE 80
