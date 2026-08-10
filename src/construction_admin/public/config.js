// Runtime configuration, overwritten inside the container at start-up.
//
// A Vite build bakes `import.meta.env.VITE_*` into the bundle, which would
// mean one image per installation — a different build for every construction
// firm the system is deployed for, and a rebuild to correct a hostname typed
// wrong. This file is read at load time instead, so the same image runs
// anywhere and the deployment supplies the addresses.
//
// Empty here on purpose: in development the `.env` file still wins, and this
// exists so the browser gets a file rather than a 404. See
// `docker/admin/entrypoint.sh` for what replaces it in a container.
window.__CONSTRUCTION_CONFIG__ = {};
