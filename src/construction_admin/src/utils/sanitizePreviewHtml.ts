/**
 * Cleans the HTML that mammoth (Word) and SheetJS (Excel) build from an uploaded file, before it is put on the page.
 *
 * The file is attacker-controlled text by the time it gets here, so this works from an allow-list wherever a value can
 * be a URL, rather than looking for known-bad words. The earlier version removed only an `href` that began with
 * exactly `javascript:`; a browser strips tabs, new lines and control characters from a URL before it reads the scheme,
 * so `jav<TAB>ascript:` passed the filter and ran when clicked.
 */

/** Elements that can run script, load other documents or draw outside the preview. */
const REMOVED_ELEMENTS = 'script, style, iframe, frame, frameset, object, embed, link, meta, base, form, svg, math, template, noscript';

/** Attributes whose value is a URL. */
const URL_ATTRIBUTES = new Set(['href', 'src', 'xlink:href', 'action', 'formaction', 'data', 'poster', 'background', 'srcset', 'ping']);

const SAFE_LINK_SCHEMES = new Set(['http:', 'https:', 'mailto:', 'tel:']);

/** A picture inside the document, which mammoth writes as a data URI. */
const SAFE_IMAGE_DATA = /^data:image\/(png|jpe?g|gif|webp|bmp);base64,[a-z0-9+/=\s]*$/i;

// What a browser ignores around and inside a URL before it reads the scheme: control characters, spaces and the
// invisible separators. Written as code points so no invisible character sits in this file.
function stripIgnored(value: string): string {
  let out = '';

  for (const character of value) {
    const code = character.codePointAt(0) ?? 0;
    const ignored =
      code <= 0x20 ||
      (code >= 0x7f && code <= 0xa0) ||
      code === 0x1680 ||
      (code >= 0x2000 && code <= 0x200f) ||
      code === 0x2028 ||
      code === 0x2029 ||
      code === 0x202f ||
      code === 0x205f ||
      code === 0x3000 ||
      code === 0xfeff;

    if (!ignored) {
      out += character;
    }
  }

  return out;
}

/** True for a value that is safe to leave in a link or picture attribute. */
export function isSafeUrl(value: string, attribute = 'href'): boolean {
  const compact = stripIgnored(value);

  if (compact === '' || compact.startsWith('#')) {
    return true;
  }

  if (attribute === 'src' && SAFE_IMAGE_DATA.test(compact)) {
    return true;
  }

  try {
    // A made-up base, so a relative address is read as relative and anything with a scheme keeps its own.
    return SAFE_LINK_SCHEMES.has(new URL(compact, 'https://preview.invalid/').protocol);
  } catch {
    return false;
  }
}

export function sanitizePreviewHtml(html: string): string {
  const parsed = new DOMParser().parseFromString(html, 'text/html');

  parsed.querySelectorAll(REMOVED_ELEMENTS).forEach((element) => element.remove());

  parsed.querySelectorAll('*').forEach((element) => {
    for (const attribute of [...element.attributes]) {
      const name = attribute.name.toLowerCase();

      if (name.startsWith('on') || name === 'srcdoc' || name === 'style') {
        element.removeAttribute(attribute.name);
      } else if (URL_ATTRIBUTES.has(name) && !isSafeUrl(attribute.value, name)) {
        element.removeAttribute(attribute.name);
      }
    }

    // A link opened from a preview must not be able to reach back into the panel.
    if (element.tagName === 'A' && element.hasAttribute('href')) {
      element.setAttribute('rel', 'noopener noreferrer');
      element.setAttribute('target', '_blank');
    }
  });

  return parsed.body.innerHTML;
}
