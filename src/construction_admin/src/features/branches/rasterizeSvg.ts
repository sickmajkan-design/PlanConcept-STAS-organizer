/** The longest side of the picture copy, in pixels: sharp enough for a spreadsheet and the phone, small enough to send. */
const RASTER_SIZE = 512;

/**
 * A png copy of an SVG logo, drawn by the browser. A spreadsheet and the phone draw pictures, not SVG, so the
 * copy goes up with the logo. Null for a file that is not an SVG, or that the browser cannot draw: the logo is
 * still saved, it just does not appear in exports.
 */
export async function rasterizeSvg(file: File): Promise<Blob | null> {
  const isSvg = file.type === 'image/svg+xml' || file.name.toLowerCase().endsWith('.svg');

  if (!isSvg) return null;

  const url = URL.createObjectURL(file);

  try {
    const image = new Image();
    image.src = url;
    await image.decode();

    const width = image.naturalWidth || RASTER_SIZE;
    const height = image.naturalHeight || RASTER_SIZE;
    const scale = RASTER_SIZE / Math.max(width, height);
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(width * scale));
    canvas.height = Math.max(1, Math.round(height * scale));

    const context = canvas.getContext('2d');

    if (!context) return null;

    context.drawImage(image, 0, 0, canvas.width, canvas.height);

    return await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/png'));
  } catch {
    return null;
  } finally {
    URL.revokeObjectURL(url);
  }
}
