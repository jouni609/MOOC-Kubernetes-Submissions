FROM node:alpine

WORKDIR /usr/src/app
COPY package* ./
RUN npm install
COPY . .
RUN npm run build

CMD ["npm", "start"]
