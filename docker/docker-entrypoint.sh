#!/bin/sh
set -e

PUID="${PUID:-1000}"
PGID="${PGID:-1000}"

if getent group dsm >/dev/null; then
  groupmod -o -g "$PGID" dsm
else
  groupadd -g "$PGID" dsm
fi

if getent passwd dsm >/dev/null; then
  usermod -o -u "$PUID" -g dsm dsm
else
  useradd -o -u "$PUID" -g dsm -d /home/dsm -m -s /bin/sh dsm
fi

if [ -n "$DOCKER_GID" ]; then
  if getent group docker >/dev/null; then
    groupmod -o -g "$DOCKER_GID" docker
  else
    groupadd -o -g "$DOCKER_GID" docker
  fi
  usermod -aG docker dsm
fi

chown -R "$PUID:$PGID" /home/dsm

# Make any mounted dashboard config dirs writable by the runtime user. Bind-mounted host dirs
# should already have the right ownership; this matters for fresh named volumes.
for d in /dashy_config /homepage_config; do
  [ -d "$d" ] && chown -R "$PUID:$PGID" "$d" 2>/dev/null || true
done

exec gosu dsm "$@"
