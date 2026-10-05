# The admin panel as a served image.
#
# The build is deliberately environment-free: no VITE_API_BASE_URL is passed
# here, because baking one in would mean a separate image per installation.
# The addresses arrive at container start through `docker/entrypoint.sh`, which
# rewrites `config.js`. One image, every deployment.

FROM node:22-alpine@sha256:0a7108bf6c7bf5de370ffb1a3ed6be93d405b43ff159f681a8d18c0e2bc2e402 AS build
WORKDIR /app

# The lockfile alone first, so a dependency install is cached across every
# change that touches only source.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# `npm run build` is `tsc -b && vite build`, so a type error fails the image
# rather than shipping.
RUN npm run build

FROM nginx:1.29-alpine@sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de AS runtime

# Two things nginx does not do usefully by default for a single-page app:
# serve index.html for a client-side route, and stay quiet about files it
# should cache forever.
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/entrypoint.sh /docker-entrypoint.d/40-construction-config.sh

COPY --from=build /app/dist /usr/share/nginx/html

# The stock nginx image runs its entrypoint scripts as root and then drops to
# the `nginx` user for the worker processes, which is what we want: the
# entrypoint has to write config.js, the workers must not be able to.
RUN chmod +x /docker-entrypoint.d/40-construction-config.sh

EXPOSE 8080
