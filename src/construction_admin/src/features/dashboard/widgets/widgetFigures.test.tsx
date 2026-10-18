/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// First of the project's own imports: it installs the fake network, and an API module loaded before it would keep
// the real one.
import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../../test/renderScreen';

import { employeesApi } from '../../../api/employees';
import { timeEntriesApi } from '../../../api/timeEntries';
import { toolsApi } from '../../../api/tools';
import type { PagedList } from '../../../api/types';
import { vehiclesApi } from '../../../api/vehicles';
import { workItemsApi } from '../../../api/workItems';

/**
 * The figures themselves: that what a widget prints is what the server counted, over the right days, and not
 * what happened to fit on the first page of a list. The suite runs in the company's time zone (see
 * src/test/globalSetup.ts), where the UTC date and the local date differ for two hours every night.
 */

let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;
const widgetProps = { instanceId: 'w1', onRemove: () => {}, onExpandWidth: () => {} };

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

afterEach(() => {
  vi.useRealTimers();
});

/** What a list endpoint answers: `totalCount` is the truth, `items` is only the page asked for. */
function page<T>(totalCount: number, items: T[] = [], pageNumber = 1, pageSize = 1): PagedList<T> {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return { items, totalCount, pageNumber, pageSize, totalPages, hasNextPage: pageNumber < totalPages, hasPreviousPage: pageNumber > 1 };
}

describe('the key figures', () => {
  it('print the server counts, not the length of a page: 120 free of 140 vehicles is 120 of 140', async () => {
    vi.spyOn(vehiclesApi, 'list').mockImplementation(async (query) => page(query.status === 'Available' ? 120 : 140));
    vi.spyOn(toolsApi, 'list').mockImplementation(async (query) => page(query.status === 'Available' ? 30 : 450));
    vi.spyOn(employeesApi, 'list').mockImplementation(async () => page(215));
    vi.spyOn(workItemsApi, 'list').mockImplementation(async (query) => page(query.overdueOnly ? 4 : 37));

    const { CompanyKpiWidget } = await import('./CompanyKpiWidget');
    renderScreen(<CompanyKpiWidget {...widgetProps} />, { user: signedIn('Admin') });

    expect(await screen.findByText('120 / 140')).toBeDefined();
    expect(screen.getByText('30 / 450')).toBeDefined();
    expect(screen.getByText('215')).toBeDefined();
    expect(screen.getByText('37')).toBeDefined();
    expect(screen.getByText('4 overdue')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('compare this month so far with the same days of last month, from the local date just after midnight', async () => {
    // 00:30 on 18 October in Belgrade; in UTC it is still 17 October 22:30.
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 9, 18, 0, 30));

    const empty = page<never>(0);
    vi.spyOn(vehiclesApi, 'list').mockResolvedValue(empty);
    vi.spyOn(toolsApi, 'list').mockResolvedValue(empty);
    vi.spyOn(employeesApi, 'list').mockResolvedValue(empty);
    vi.spyOn(workItemsApi, 'list').mockResolvedValue(empty);
    network.reply('/costs/company', 200, { total: 100, labour: 0, manualPay: 0 });

    const { CompanyKpiWidget } = await import('./CompanyKpiWidget');
    renderScreen(<CompanyKpiWidget {...widgetProps} />, { user: { ...signedIn('Admin'), financeAccess: 'Full' } });

    await waitFor(() => {
      const windows = network.calls.filter((c) => c.url.includes('/costs/company')).map((c) => `${c.params.from}..${c.params.to}`);
      expect(windows).toContain('2026-10-01..2026-10-18');
      expect(windows).toContain('2026-09-01..2026-09-18');
    });

    // Against the whole of September, a month that is eighteen days old would have looked like a fall.
    const windows = network.calls.filter((c) => c.url.includes('/costs/company')).map((c) => `${c.params.from}..${c.params.to}`);
    expect(windows).not.toContain('2026-09-01..2026-09-30');
  }, SCREEN_TIMEOUT);
});

describe('the fleet', () => {
  it('counts every status on the server, so a fleet larger than a page is not cut at a hundred', async () => {
    vi.spyOn(vehiclesApi, 'list').mockImplementation(async (query) =>
      page(({ Available: 90, Assigned: 40, InService: 7, OutOfService: 3, RentedOut: 0 } as Record<string, number>)[query.status ?? ''] ?? 140),
    );
    vi.spyOn(toolsApi, 'list').mockImplementation(async () => page(0));

    const { FleetStatusWidget } = await import('./FleetStatusWidget');
    renderScreen(<FleetStatusWidget {...widgetProps} />, { user: signedIn('Admin') });

    expect(await screen.findByText('Vehicles · 140')).toBeDefined();
    // 90 + 40 + 7 + 3 is the whole fleet; a page of a hundred would have shown at most 100 of them.
    expect(screen.getByText('90')).toBeDefined();
    expect(screen.getByText('40')).toBeDefined();
  }, SCREEN_TIMEOUT);
});

describe('who is on site', () => {
  const entry = (n: number, startedAt: string) => ({
    id: `e${n}`,
    employeeName: `Radnik ${n}`,
    projectName: n % 2 === 0 ? 'Zemun' : 'Vrbas',
    startedAt,
    breakMinutes: 0,
  });

  it('reads every page of running shifts: 130 clocked in is 130, not 100', async () => {
    const startedAt = new Date(Date.now() - 60 * 60_000).toISOString();
    const all = Array.from({ length: 130 }, (_, i) => entry(i, startedAt));
    vi.spyOn(timeEntriesApi, 'list').mockImplementation(async (query) => {
      const size = query.pageSize ?? 100;
      const from = ((query.pageNumber ?? 1) - 1) * size;

      return page(130, all.slice(from, from + size) as never[], query.pageNumber ?? 1, size);
    });
    vi.spyOn(employeesApi, 'list').mockImplementation(async () => page(200));
    network.reply('/timeentries/summary', 200, { totalMinutes: 0, pendingCount: 0 });

    const { TodayAttendanceWidget } = await import('./TodayAttendanceWidget');
    renderScreen(<TodayAttendanceWidget {...widgetProps} />, { user: signedIn('Admin') });

    expect(await screen.findByText('130 on site now')).toBeDefined();
    expect(screen.getByText('of 200')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('adds the time shifts still running to the hours finished today', async () => {
    const now = new Date(2026, 9, 18, 15, 0);
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(now);

    // One shift began at 13:30 and is still running: an hour and a half so far.
    vi.spyOn(timeEntriesApi, 'list').mockImplementation(async () =>
      page(1, [entry(1, new Date(2026, 9, 18, 13, 30).toISOString())] as never[], 1, 100),
    );
    vi.spyOn(employeesApi, 'list').mockImplementation(async () => page(10));
    // And 60 minutes are finished and in the server's summary.
    network.reply('/timeentries/summary', 200, { totalMinutes: 60, pendingCount: 0 });

    const { TodayAttendanceWidget } = await import('./TodayAttendanceWidget');
    renderScreen(<TodayAttendanceWidget {...widgetProps} />, { user: signedIn('Admin') });

    expect(await screen.findByText('2h 30m today')).toBeDefined();

    // The day asked for is the local one: midnight to midnight in Belgrade, not the UTC date.
    const summary = network.calls.find((c) => c.url.includes('/timeentries/summary'))!;
    expect(summary.params.from).toBe(new Date(2026, 9, 18).toISOString());
    expect(summary.params.to).toBe(new Date(2026, 9, 19).toISOString());
  }, SCREEN_TIMEOUT);
});

describe('realization of the contracts', () => {
  it('draws the share of the contract as a percentage: a project at 98% is a nearly full bar', async () => {
    network.reply('/projects/annual-realization', 200, {
      year: 2026,
      rows: [
        { projectId: 'p1', projectName: 'Fasada', status: 'Active', contractValue: 130000, realizedThisYear: 127970.45, realizedToDate: 127970.45, remaining: 2029.55, percentOfContract: 0.984388 },
        { projectId: 'p2', projectName: 'Vrbas', status: 'Active', contractValue: 95000, realizedThisYear: 151171.14, realizedToDate: 151171.14, remaining: -56171.14, percentOfContract: 1.591275 },
      ],
      totalContractValue: 225000,
      totalRealizedThisYear: 279141.59,
      totalRealizedToDate: 279141.59,
      totalRemaining: -54141.59,
      percentRealized: 1.2406,
    });
    network.reply('/projectrevenues', 200, page(0, [], 1, 200));
    network.reply('/costs/company', 200, { total: 0 });

    const { ProjectsRealizationWidget } = await import('./ProjectsRealizationWidget');
    renderScreen(<ProjectsRealizationWidget {...widgetProps} />, { user: { ...signedIn('Admin'), financeAccess: 'Full' } });

    await screen.findByText('Fasada');
    const bars = screen.getAllByRole('progressbar').map((bar) => Number(bar.getAttribute('aria-valuenow')));

    // Largest first: the overrun is shown as full (the bar stops at 100), the other as 98 of 100.
    expect(bars.slice(0, 2)).toEqual([100, 98]);
  }, SCREEN_TIMEOUT);
});
