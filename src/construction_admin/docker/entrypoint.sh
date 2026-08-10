#!/bin/sh
#
# Writes the runtime configuration the app reads before its bundle.
#
# Runs from nginx's own /docker-entrypoint.d, which the stock image executes as
# root before dropping to the worker user — so this can write the file and the
# workers cannot.
#
# The point of it: `vite build` bakes `import.meta.env.VITE_*` into the bundle,
# so a build knows one API address for ever. Every installation would need its
# own image, and a hostname typed wrong would mean a release rather than a
# restart. This is read at load time instead.

set -eu

target=/usr/share/nginx/html/config.js

# An unset variable becomes an empty string, and the app treats empty as "not
# configured" and falls back. That is deliberate: a deployment that forgets
# API_BASE_URL should show an app that cannot reach its server, not an app
# quietly pointed at localhost with no explanation.
api_base_url=${API_BASE_URL:-}
google_maps_api_key=${GOOGLE_MAPS_API_KEY:-}

# Escaped, because these values arrive from the environment and are written
# into JavaScript. A quote or a backslash in a hostname would otherwise end the
# string and turn a typo into script.
escape() {
    printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'
}

cat > "$target" <<CONFIG
// Generated at container start by 40-construction-config.sh. Do not edit:
// this file is overwritten on every restart.
window.__CONSTRUCTION_CONFIG__ = {
  apiBaseUrl: "$(escape "$api_base_url")",
  googleMapsApiKey: "$(escape "$google_maps_api_key")"
};
CONFIG

if [ -z "$api_base_url" ]; then
    echo "construction-admin: API_BASE_URL is not set; the panel will not reach an API." >&2
fi
