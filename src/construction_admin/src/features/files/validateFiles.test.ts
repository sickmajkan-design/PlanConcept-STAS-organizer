import { describe, expect, it } from 'vitest';

import { validateFiles } from './validateFiles';

const file = (name: string, size = 100, lastModified = 1, type = 'application/pdf') =>
  new File([new Uint8Array(size)], name, { type, lastModified });

const rules = { accept: '.pdf,.jpg,.png', maxBytes: 1000 };

describe('validateFiles', () => {
  it('takes files of an allowed type within the size', () => {
    const { accepted, rejected } = validateFiles([file('a.pdf'), file('b.JPG', 10, 2, 'image/jpeg')], rules);

    expect(accepted.map((f) => f.name)).toEqual(['a.pdf', 'b.JPG']);
    expect(rejected).toEqual([]);
  });

  it('turns away a file over the limit and keeps the rest', () => {
    const { accepted, rejected } = validateFiles([file('big.pdf', 1001), file('ok.pdf')], rules);

    expect(accepted.map((f) => f.name)).toEqual(['ok.pdf']);
    expect(rejected.map((r) => [r.file.name, r.reason])).toEqual([['big.pdf', 'size']]);
  });

  it('accepts a file of exactly the limit', () => {
    expect(validateFiles([file('edge.pdf', 1000)], rules).accepted).toHaveLength(1);
  });

  it('turns away an extension that is not allowed, whatever the case', () => {
    const { rejected } = validateFiles([file('x.EXE', 10, 1, 'application/x-msdownload'), file('noext')], rules);

    expect(rejected.map((r) => r.reason)).toEqual(['type', 'type']);
  });

  it('turns away an empty file, and a folder, which arrives as an empty untyped file', () => {
    const folder = new File([], 'Dokumenti', { type: '' });
    const { rejected } = validateFiles([file('nula.pdf', 0), folder], rules);

    expect(rejected.map((r) => r.reason)).toEqual(['empty', 'empty']);
  });

  it('turns away a repeat of a file already taken, and a repeat inside the same drop', () => {
    const first = file('a.pdf', 50, 7);
    const { accepted, rejected } = validateFiles([file('a.pdf', 50, 7), file('b.pdf'), file('b.pdf')], {
      ...rules,
      existing: [first],
    });

    expect(accepted.map((f) => f.name)).toEqual(['b.pdf']);
    expect(rejected.map((r) => r.reason)).toEqual(['duplicate', 'duplicate']);
  });

  it('keeps two different files that share a name', () => {
    const { accepted } = validateFiles([file('a.pdf', 50, 1), file('a.pdf', 60, 1)], rules);

    expect(accepted).toHaveLength(2);
  });

  it('stops at the most files allowed, counting those already taken', () => {
    const { accepted, rejected } = validateFiles([file('a.pdf', 1, 1), file('b.pdf', 2, 1), file('c.pdf', 3, 1)], {
      ...rules,
      maxFiles: 2,
      existing: [file('old.pdf', 9, 1)],
    });

    expect(accepted.map((f) => f.name)).toEqual(['a.pdf']);
    expect(rejected.map((r) => [r.file.name, r.reason])).toEqual([
      ['b.pdf', 'count'],
      ['c.pdf', 'count'],
    ]);
  });
});
