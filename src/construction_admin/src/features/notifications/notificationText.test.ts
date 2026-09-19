import { describe, expect, it } from 'vitest';

import type { Notification } from '../../api/types';
import { en } from '../../i18n/en';
import { sr } from '../../i18n/sr';
import { resolveNotificationText } from './notificationText';

function makeT(dictionary: Record<string, unknown>) {
  return ((key: string, values?: Record<string, string | number>) => {
    const message = dictionary[key] as string;
    return message.replace(/\{(\w+)\}/g, (_m, name: string) => String(values?.[name] ?? `{${name}}`));
  }) as never;
}

const base = {
  id: '1',
  title: 'Cost sent back',
  body: 'server english',
  isRead: false,
  createdAt: '2026-09-19T10:00:00Z',
} as unknown as Notification;

const rejected = {
  ...base,
  type: 'VehicleExpenseRejected',
  dataJson: JSON.stringify({ vehicleName: 'Iveco', occurredOn: '2026-09-18', note: 'Nema računa' }),
} as Notification;

describe('resolveNotificationText', () => {
  it('rebuilds the sentence in the reader\'s language from the stored facts', () => {
    expect(resolveNotificationText(makeT(sr), rejected)).toEqual({
      title: 'Trošak vraćen na doradu',
      body: 'Iveco (18.09.2026.) je vraćen: Nema računa',
    });
    expect(resolveNotificationText(makeT(en), rejected).title).toBe('Cost sent back');
  });

  it('falls back to what the server wrote when a fact is missing', () => {
    const broken = { ...rejected, dataJson: JSON.stringify({ vehicleName: 'Iveco' }) } as Notification;

    expect(resolveNotificationText(makeT(sr), broken)).toEqual({
      title: 'Cost sent back',
      body: 'server english',
    });
  });

  it('shows free text exactly as sent', () => {
    const announcement = { ...base, type: 'GeneralAnnouncement', title: 'Zbor', body: 'Sutra u 8' } as Notification;

    expect(resolveNotificationText(makeT(sr), announcement)).toEqual({ title: 'Zbor', body: 'Sutra u 8' });
  });

  it('counts costs when several were imported at once', () => {
    const bulk = { ...base, type: 'VehicleExpenseSubmitted', dataJson: JSON.stringify({ count: '12' }) } as Notification;

    expect(resolveNotificationText(makeT(sr), bulk).body).toBe('12 troškova čeka pregled.');
  });
});
