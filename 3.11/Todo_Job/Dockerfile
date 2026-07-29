FROM google/cloud-sdk:alpine

RUN apk add --no-cache postgresql16-client bash

WORKDIR /app
COPY backup.sh /app/backup.sh
RUN chmod +x /app/backup.sh

ENTRYPOINT ["/app/backup.sh"]
