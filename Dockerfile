FROM node:14.15.4-alpine

# Should be built with `docker build -p 3000:3000 --init --network host imagename`
# Then run with `docker run imagename`
#
# For green-blue:
# docker build -p 3000:3000 --init --network host imagename-green
# docker build -p 3001:3000 --init --network host imagename-blue
# Then edit Apache config to forward port 3001 or port 3000, and do `systemctl reload apache2`

WORKDIR /app
COPY package*.json .env ./
RUN npm ci
COPY build/* ./
COPY static assets
EXPOSE 3000
USER node
ENTRYPOINT ["node", "index.js"]
