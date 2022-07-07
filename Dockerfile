FROM ruby:3.1-alpine3.16 AS builder

ADD Gemfile* .
RUN apk upgrade && \
    apk add --no-cache \
      g++ \
      make \
      linux-headers \
      libstdc++ && \
    bundle install

FROM ruby:3.1-alpine3.16

ARG PUID=1000
ARG PGID=1000
ARG DOCKER_GID=998

ENV APP_HOME /app

RUN addgroup -g $PGID ruby && \
    addgroup -g $DOCKER_GID docker && \
    adduser --system --shell /bin/ash --home /home/ruby --uid $PUID --ingroup ruby ruby && \
    addgroup ruby docker && \
    apk update

RUN mkdir $APP_HOME && \
    chown -R ruby:ruby $APP_HOME
WORKDIR $APP_HOME

USER ruby

COPY --from=builder /usr/local/bundle/ /usr/local/bundle/

ADD --chown=ruby:ruby . $APP_HOME/

EXPOSE 50999

CMD ["bundle", "exec", "rackup", "--host", "0.0.0.0", "--port", "50999" ]