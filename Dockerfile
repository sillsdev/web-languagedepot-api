FROM node:14.15.4-alpine AS builder

# Should be built with `npm run docker`
# Then run with `docker run --init --env-file=.env --network host docker.dallas.languagetechnology.org/node-ldapi:latest`
#
# This assumes a .env file that looks something like this:
# PORT=3000
# MYSQL_USER=mysqlusername
# MYSQL_PASSWORD=mysqlpassword

RUN apk add dumb-init
RUN npm i -g pnpm@6.6.2
WORKDIR /app
COPY package.json pnpm-lock.yaml ./
RUN pnpm i
COPY static ./static
COPY svelte.config.js tsconfig.json ./
COPY src ./src
RUN pnpm run build

FROM node:14.15.4-alpine
COPY --from=builder /usr/bin/dumb-init /usr/bin/dumb-init
COPY --from=builder /usr/local/lib/node_modules/pnpm /usr/local/lib/node_modules/pnpm
RUN ln -s ../lib/node_modules/pnpm/bin/pnpm.cjs /usr/local/bin/pnpm && \
    ln -s ../lib/node_modules/pnpm/bin/pnpx.cjs /usr/local/bin/pnpx
WORKDIR /app
COPY static assets
COPY package.json pnpm-lock.yaml ./
RUN pnpm i --prod --frozen-lockfile
COPY --from=builder app/build ./
EXPOSE 3000
USER node
ENTRYPOINT ["dumb-init"]
CMD ["node", "index.js"]
