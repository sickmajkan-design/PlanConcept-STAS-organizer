const GIGABYTE = 1024 * 1024 * 1024;
const MEGABYTE = 1024 * 1024;

/** `7.6 GB` for a disk, `52 MB` for a folder's worth: one decimal above a gigabyte, none below. */
export function formatSize(bytes: number): string {
  return bytes >= GIGABYTE ? `${(bytes / GIGABYTE).toFixed(1)} GB` : `${Math.round(bytes / MEGABYTE)} MB`;
}
