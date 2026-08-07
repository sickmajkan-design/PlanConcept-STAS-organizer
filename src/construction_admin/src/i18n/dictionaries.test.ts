import { describe, expect, it } from 'vitest';

import { en } from './en';
import { sr } from './sr';
import type { Message } from './types';

/**
 * The two dictionaries, checked against each other.
 *
 * TypeScript already forces `sr` to declare every key `en` has. What it cannot
 * see is the inside of a message: a translation that lost a `{count}`, or that
 * is a plain string where the English is a plural set, compiles perfectly and
 * shows a placeholder-free sentence with the number missing.
 */
const keys = Object.keys(en) as (keyof typeof en)[];

function placeholdersOf(message: Message): Set<string> {
  const text =
    typeof message === 'string'
      ? message
      : [message.one, message.few ?? '', message.other].join(' ');

  return new Set(Array.from(text.matchAll(/\{(\w+)\}/g), (match) => match[1]!));
}

describe('the Serbian dictionary', () => {
  it('has an entry for every English key and no others', () => {
    expect(Object.keys(sr).sort()).toEqual(keys.slice().sort());
  });

  it('has no empty translations', () => {
    const empty = keys.filter((key) => {
      const message = sr[key];

      return typeof message === 'string'
        ? message.trim().length === 0
        : [message.one, message.other].some((form) => form.trim().length === 0);
    });

    expect(empty).toEqual([]);
  });

  it('interpolates the same values as the English', () => {
    const mismatched = keys.filter((key) => {
      const english = placeholdersOf(en[key]);
      const serbian = placeholdersOf(sr[key]);

      return (
        english.size !== serbian.size ||
        [...english].some((name) => !serbian.has(name))
      );
    });

    // A translation missing `{count}` renders a sentence with the number gone
    // — grammatical, plausible, and wrong.
    expect(mismatched).toEqual([]);
  });

  it('is a plural set wherever the English is one', () => {
    const wrongShape = keys.filter(
      (key) => typeof en[key] !== typeof sr[key],
    );

    // Serbian needs three forms where English needs two, so it can never need
    // fewer. A plain string here would print "5 dan" for every count.
    expect(wrongShape).toEqual([]);
  });

  it('gives every plural set the `few` form Serbian requires', () => {
    const missingFew = keys.filter((key) => {
      const message = sr[key];

      return typeof message !== 'string' && message.few === undefined;
    });

    // `few` is optional in the type because English does not have one. In
    // Serbian, 2–4 is its own form: "2 dana", not "2 dan" and not "2 dana"
    // by accident.
    expect(missingFew).toEqual([]);
  });
});

describe('the English dictionary', () => {
  it('declares both forms on every plural set', () => {
    const incomplete = keys.filter((key) => {
      const message = en[key];

      return (
        typeof message !== 'string' &&
        (message.one.length === 0 || message.other.length === 0)
      );
    });

    expect(incomplete).toEqual([]);
  });

  it('is not empty, so a broken import cannot pass this file', () => {
    expect(keys.length).toBeGreaterThan(100);
  });
});
