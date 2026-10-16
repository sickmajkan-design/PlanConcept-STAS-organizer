import { describe, expect, it } from 'vitest';

import sr from './sr.ts?raw';

/**
 * The panel speaks to its readers formally ("vi"). Texts for workers (notifications they receive) and the
 * assistant are the exception and are left out. A text that slips into the singular "you" fails here.
 */
describe('Serbian panel texts', () => {
  const singular = /\b(tvoj\w*|tebi|biraš|izabereš|upišeš|vidiš|možeš|Možeš|imaš|plaćaš|isplaćuješ|upisuješ|ispraviš|potvrdiš|pogledaš|pročitaš|pomjeriš)\b/;

  it('does not address the reader as "ti"', () => {
    const problems = sr
      .split('\n')
      .map((line, index) => ({ line, number: index + 1 }))
      .filter(({ line }) => /^\s*'[^']+':/.test(line) && !/^\s*'(notificationText|assistant)\./.test(line) && singular.test(line))
      .map(({ line, number }) => `${number}: ${line.trim().slice(0, 100)}`);

    expect(problems).toEqual([]);
  });
});
