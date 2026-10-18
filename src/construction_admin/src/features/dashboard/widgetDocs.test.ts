import { describe, expect, it } from 'vitest';

import en from '../../i18n/en.ts?raw';
import sr from '../../i18n/sr.ts?raw';
import { helpTopicById } from '../help/helpTopics';
import { widgetRegistry } from './widgetRegistry';
import { dashboardWidgetTypes } from './widgetTypes';

/**
 * Every widget says what it is for and how its figures are counted: a topic in Help, a link to it on the card and a
 * line in the picker. A widget added without them fails here rather than reaching people as numbers nobody can explain.
 */
describe('widget documentation', () => {
  it('has a help topic for every widget, with one entry in it for each widget that points to it', () => {
    const pointingAt = new Map<string, string[]>();

    for (const type of dashboardWidgetTypes) {
      const topic = widgetRegistry[type].help;
      pointingAt.set(topic, [...(pointingAt.get(topic) ?? []), type]);
    }

    expect(pointingAt.size).toBeGreaterThan(0);

    for (const [id, types] of pointingAt) {
      const topic = helpTopicById(id);

      expect(topic, id).toBeDefined();
      // One step per widget, in both languages: Help cannot lag behind the board in one of them.
      expect(topic!.content.sr.steps.length, `${id} sr: ${types.join(', ')}`).toBe(types.length);
      expect(topic!.content.en.steps.length, `${id} en: ${types.join(', ')}`).toBe(types.length);
    }
  });

  it('says in the picker what each widget is for and where to read how it is counted, in both languages', () => {
    for (const type of dashboardWidgetTypes) {
      for (const [language, source] of [['sr', sr], ['en', en]] as const) {
        // A text in single quotes; a quote inside it is written \'.
        const text = String.raw`'((?:[^'\\]|\\.)+)'`;
        const info = new RegExp(`'dashboard\\.widgetInfo\\.${type}': ${text}`).exec(source);
        const setup = new RegExp(`'dashboard\\.widgetSetup\\.${type}': ${text}`).exec(source);

        expect(info, `${language} ${type} purpose`).not.toBeNull();
        expect(setup?.[1], `${language} ${type} pointer to Help`).toMatch(/Pomoć, tema|Help, the topic/);
      }
    }
  });
});
