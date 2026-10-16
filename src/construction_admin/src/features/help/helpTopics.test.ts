import { describe, expect, it } from 'vitest';

import type { User } from '../../api/types';
import serverMore from '../../i18n/serverMessages.more.sr.ts?raw';
import topicsSource from './helpTopics.ts?raw';
import { helpGroups, helpMatches, helpTopicById, helpTopics, helpTopicsFor } from './helpTopics';

const user = (role: User['role'], extra: Partial<User> = {}) => ({ role, ...extra }) as User;

describe('help topics', () => {
  it('have unique ids, a known group and a screen to open', () => {
    const ids = helpTopics.map((topic) => topic.id);

    expect(new Set(ids).size).toBe(ids.length);

    for (const topic of helpTopics) {
      expect(helpGroups.map((g) => g.id), topic.id).toContain(topic.group);
      expect(topic.path.startsWith('/'), topic.id).toBe(true);
    }
  });

  it('are written in both languages, with steps, an example and something worth knowing', () => {
    for (const topic of helpTopics) {
      for (const lang of ['sr', 'en'] as const) {
        const content = topic.content[lang];

        expect(content.title.length, `${topic.id} ${lang} title`).toBeGreaterThan(3);
        expect(content.summary.length, `${topic.id} ${lang} summary`).toBeGreaterThan(30);
        expect(content.steps.length, `${topic.id} ${lang} steps`).toBeGreaterThanOrEqual(3);
        expect(content.example?.length ?? 0, `${topic.id} ${lang} example`).toBeGreaterThan(30);
        expect(content.tips.length, `${topic.id} ${lang} tips`).toBeGreaterThanOrEqual(1);
        expect(content.keywords.length, `${topic.id} ${lang} keywords`).toBeGreaterThan(3);
      }

      // The same number of steps in both, so one language is never the thinner one.
      expect(topic.content.sr.steps.length, topic.id).toBe(topic.content.en.steps.length);
    }
  });

  it('are found by a related word as well as by the title', () => {
    const upload = helpTopicById('dokumenti-dodavanje')!;

    expect(helpMatches(upload.content.sr, 'otpremi')).toBe(true);
    expect(helpMatches(upload.content.sr, 'prevuci pdf')).toBe(true);
    expect(helpMatches(upload.content.en, 'drag drop')).toBe(true);
    expect(helpMatches(upload.content.sr, 'fakture porez')).toBe(false);
  });

  it('are offered only to those who can open the screen they describe', () => {
    const forWorker = helpTopicsFor(user('Worker')).map((t) => t.id);
    const forAdmin = helpTopicsFor(user('Admin')).map((t) => t.id);
    const forSuper = helpTopicsFor(user('SuperAdmin')).map((t) => t.id);

    expect(forWorker).toContain('pretraga-precice');
    expect(forWorker).not.toContain('zahtjevi-dokumenti');
    expect(forAdmin).toContain('zahtjevi-dokumenti');
    // The book is closed to an admin until it is granted.
    expect(forAdmin).not.toContain('knjiga-pregled');
    expect(forSuper).toContain('knjiga-pregled');
    expect(helpTopicsFor(user('Admin', { bookAccess: true }))).toContain(helpTopicById('knjiga-pregled'));
  });
});

/**
 * The platform is in Serbian, ijekavian. These are the forms that give away Croatian or ekavian text; the
 * existing texts have none, and a new one that does fails here instead of reaching the client.
 */
describe('Serbian text', () => {
  const forbidden: [RegExp, string][] = [
    [/\btko\b/i, 'tko (ko)'],
    [/\btjed(an|na|ni|nu)\b/i, 'tjedan (sedmica)'],
    [/\bposlužitelj/i, 'poslužitelj (server)'],
    [/\bkruh\b/i, 'kruh'],
    [/\bizvješć/i, 'izvješće (izvještaj)'],
    [/\btisuć/i, 'tisuća (hiljada)'],
    [/\bzdravstven/i, 'zdravstveni (ne mora, ali provjeriti)'],
    [/\bmleko\b|\bvreme\b|\bmesec\b|\blepo\b|\bposle\b|\bdete\b/i, 'ekavski oblik'],
  ];

  const files: [string, string][] = [
    ['serverMessages.more.sr.ts', serverMore],
    ['helpTopics.ts', topicsSource],
  ];

  it('has no Croatian or ekavian forms in the guides and the server messages', () => {
    const problems: string[] = [];

    for (const [file, text] of files) {

      for (const [pattern, label] of forbidden) {
        const match = pattern.exec(text);

        if (match) problems.push(`${file}: ${label} -> ${match[0]}`);
      }
    }

    expect(problems).toEqual([]);
  });
});
