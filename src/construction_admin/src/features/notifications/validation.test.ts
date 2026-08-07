import { describe, expect, it } from 'vitest';

import { announcementFormSchema } from './validation';

const valid = {
  title: 'Struja se gasi u 10h',
  body: 'Radovi na trafostanici.',
  role: '',
  projectId: '',
};

/**
 * The one form in the panel whose mistakes cannot be taken back: an
 * announcement reaches every matching phone the moment it is sent.
 */
describe('the announcement form', () => {
  it('accepts a subject and a body addressed to everyone', () => {
    expect(announcementFormSchema.safeParse(valid).success).toBe(true);
  });

  it('refuses a blank subject, including one made of spaces', () => {
    expect(announcementFormSchema.safeParse({ ...valid, title: '' }).success).toBe(false);
    expect(announcementFormSchema.safeParse({ ...valid, title: '   ' }).success).toBe(false);
  });

  it('refuses a blank message', () => {
    expect(announcementFormSchema.safeParse({ ...valid, body: '  ' }).success).toBe(false);
  });

  it('holds the API s length limits, so a rejection is caught before sending', () => {
    expect(
      announcementFormSchema.safeParse({ ...valid, title: 'x'.repeat(256) }).success,
    ).toBe(true);
    expect(
      announcementFormSchema.safeParse({ ...valid, title: 'x'.repeat(257) }).success,
    ).toBe(false);
    expect(
      announcementFormSchema.safeParse({ ...valid, body: 'x'.repeat(4001) }).success,
    ).toBe(false);
  });

  it('accepts a real role and refuses one the API does not have', () => {
    expect(announcementFormSchema.safeParse({ ...valid, role: 'Foreman' }).success)
      .toBe(true);
    expect(announcementFormSchema.safeParse({ ...valid, role: 'Investor' }).success)
      .toBe(false);
  });

  it('reads an empty role as "everyone" rather than as a missing answer', () => {
    // A `<TextField select>` cannot hold null, so the empty option carries
    // "no filter". It is turned back into null on submit.
    const parsed = announcementFormSchema.parse({ ...valid, role: '' });

    expect(parsed.role).toBe('');
  });

  it('trims the text it passes on', () => {
    const parsed = announcementFormSchema.parse({
      ...valid,
      title: '  Struja  ',
      body: '  Radovi.  ',
    });

    expect(parsed.title).toBe('Struja');
    expect(parsed.body).toBe('Radovi.');
  });
});
