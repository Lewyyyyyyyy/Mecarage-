FROM node:22-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime

ARG API_BASE_URL=/api

RUN rm -f /etc/nginx/conf.d/default.conf \
    && cat <<'EOF' > /etc/nginx/conf.d/default.conf
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location ~* \.(?:css|js|ico|png|jpg|jpeg|gif|svg|woff2?)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        access_log off;
    }
}
EOF

COPY --from=build /app/dist/MecaRage/browser /usr/share/nginx/html
RUN find /usr/share/nginx/html -type f \( -name '*.js' -o -name '*.html' \) -exec sed -i "s|http://localhost:5073/api|${API_BASE_URL}|g" {} +

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

