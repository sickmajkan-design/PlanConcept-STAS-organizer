/**
 * @vitest-environment jsdom
 */
import { describe, expect, it } from 'vitest';
import { isSafeUrl, sanitizePreviewHtml } from './sanitizePreviewHtml';

const hrefOf = (html: string) => {
  const holder = document.createElement('div');
  holder.innerHTML = sanitizePreviewHtml(html);
  return holder.querySelector('a')?.getAttribute('href') ?? null;
};

describe('sanitizePreviewHtml', () => {
  // The six links of the crafted Word file used to prove the problem in the browser.
  it.each([
    ['plain', 'javascript:alert(1)'],
    ['upper case', 'JaVaScRiPt:alert(1)'],
    ['a tab inside the scheme', 'jav\tascript:alert(1)'],
    ['a new line inside the scheme', 'java\nscript:alert(1)'],
    ['a control character in front', '\u0001javascript:alert(1)'],
    ['a data page', 'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg=='],
    ['vbscript', 'vbscript:msgbox(1)'],
    ['a space in front', '  javascript:alert(1)'],
  ])('removes a link that hides script: %s', (_name, href) => {
    expect(hrefOf(`<p><a href="${href}">x</a></p>`)).toBeNull();
  });

  it.each([
    ['https://example.com/a?b=1'],
    ['http://example.com'],
    ['mailto:office@example.com'],
    ['tel:+38733123456'],
    ['#section'],
    ['/relative/path'],
  ])('keeps an ordinary link: %s', (href) => {
    expect(hrefOf(`<a href="${href}">x</a>`)).toBe(href);
  });

  it('makes every link open elsewhere, without access to the panel', () => {
    const holder = document.createElement('div');
    holder.innerHTML = sanitizePreviewHtml('<a href="https://example.com">x</a>');
    const link = holder.querySelector('a');

    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link?.getAttribute('target')).toBe('_blank');
  });

  it('removes script-bearing elements and handlers', () => {
    const html = sanitizePreviewHtml(
      '<p onclick="x()">a</p><script>x()</script><iframe src="https://e.x"></iframe><svg onload="x()"></svg>' +
        '<form action="javascript:x()"><button formaction="javascript:x()">go</button></form><style>p{}</style>',
    );

    expect(html).not.toMatch(/script|iframe|svg|form|onclick|onload|style/i);
    expect(html).toContain('a');
  });

  it('keeps a picture embedded as a data address, and drops other data addresses', () => {
    expect(sanitizePreviewHtml('<img src="data:image/png;base64,iVBORw0KGgo=">')).toContain('data:image/png');
    expect(sanitizePreviewHtml('<img src="data:text/html;base64,PHNjcmlwdD4=">')).not.toContain('data:text/html');
    expect(sanitizePreviewHtml('<img src="javascript:x()">')).not.toContain('javascript');
  });

  it('keeps ordinary document content', () => {
    const html = sanitizePreviewHtml('<table><tbody><tr><td colspan="2">Zbir</td></tr></tbody></table><h1>Naslov</h1>');

    expect(html).toContain('<td colspan="2">Zbir</td>');
    expect(html).toContain('<h1>Naslov</h1>');
  });
});

describe('isSafeUrl', () => {
  it('reads the scheme the way a browser does', () => {
    expect(isSafeUrl('jav\tascript:alert(1)')).toBe(false);
    expect(isSafeUrl('https://example.com')).toBe(true);
    expect(isSafeUrl('')).toBe(true);
  });
});
