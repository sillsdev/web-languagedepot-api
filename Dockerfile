FROM node:14.15.4-alpine AS builder

# Should be built with `npm run docker`
# Then run with `docker run --init --env-file=.env --network host docker.dallas.languagetechnology.org/node-ldapi:latest`
#
# This assumes a .env file that looks something like this:
# PORT=3000
# MYSQL_USER=mysqlusername
# MYSQL_PASSWORD=mysqlpassword

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY static ./static
COPY svelte.config.js snowpack.config.js tsconfig.json ./
COPY src ./src
RUN npm run build && npm run adapt

FROM node:14.15.4-alpine
WORKDIR /app
COPY static assets
COPY package*.json ./
RUN npm ci
COPY --from=builder app/build ./
EXPOSE 3000
USER node
ENTRYPOINT ["node", "index.js"]
