FROM ruby:3.1-alpine3.16 AS builder

ADD src/Gemfile* .
RUN apk upgrade && \
    apk add --no-cache \
      g++ \
      make && \
    bundle config set with api && \
    bundle install

FROM ruby:3.1-alpine3.16

ARG PUID=1000
ARG PGID=1000
ARG DOCKER_GID=998

ENV APP_HOME /app
ENV APP_CONFIG_DIR /config

RUN addgroup -g $PGID ruby && \
    addgroup -g $DOCKER_GID docker && \
    adduser --system --shell /bin/ash --home /home/ruby --uid $PUID --ingroup ruby ruby && \
    addgroup ruby docker && \
    apk update

RUN mkdir $APP_HOME && \
    mkdir $APP_CONFIG_DIR && \
    chown ruby:ruby $APP_HOME && \
    chown ruby:ruby $APP_CONFIG_DIR && \
    bundle config set with api

WORKDIR $APP_HOME

USER ruby

COPY --from=builder /usr/local/bundle/ /usr/local/bundle/

ADD --chown=ruby:ruby src/ $APP_HOME/

EXPOSE 59999

VOLUME $APP_CONFIG_DIR

CMD ["bundle", "exec", "rackup", "--host", "0.0.0.0", "--port", "59999", "--env", "development" ]