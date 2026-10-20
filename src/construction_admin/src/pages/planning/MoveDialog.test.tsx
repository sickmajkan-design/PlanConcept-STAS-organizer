/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import type { PlanningData } from '../../api/planning';
import { buildPlan } from '../../features/planning/planningLogic';
import { installFakeNetwork, renderScreen, signedIn } from '../../test/renderScreen';
import type { PlanningActions } from './PlanningDialogs';

/**
 * Dropping a person is only a way to choose who and where: the window must name the site and the dates,
 * change nothing until it is confirmed, and then ask for exactly the same assign or swap as a click does.
 */
const HALL = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const BRIDGE = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

// A Wednesday.
const now = new Date(2026, 9, 14, 10, 0, 0);
const pad = (n: number) => String(n).padStart(2, '0');
const local = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const addDays = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
const today = local(now);

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(now);
  installFakeNetwork();
});

afterEach(() => {
  vi.useRealTimers();
});

function site(id: string, name: string, needs: { position: string; count: number }[]) {
  return { id, name, status: 'Active', customerName: null, startDate: null, endDate: null, worksSaturdays: false, worksSundays: false, needs };
}

function person(id: string, fullName: string, position: string, projectId: string | null) {
  return {
    id,
    fullName,
    position,
    postings: projectId ? [{ projectId, startDate: local(addDays(now, -30)), endDate: null }] : [],
    absences: [],
  };
}

const data = {
  from: local(addDays(now, -31)),
  to: local(addDays(now, 60)),
  today,
  positions: ['Zidar'],
  projects: [site(HALL, 'Aldi Hall', [{ position: 'Zidar', count: 2 }]), site(BRIDGE, 'Sava Bridge', [])],
  employees: [person('e1', 'Ana Novak', 'Zidar', HALL), person('e2', 'Ivo Babić', 'Zidar', null), person('e3', 'Marko Horvat', 'Zidar', BRIDGE)],
} as unknown as PlanningData;

function actions(): PlanningActions {
  return { assign: vi.fn().mockResolvedValue(undefined), swap: vi.fn().mockResolvedValue(undefined), saveNeeds: vi.fn().mockResolvedValue(undefined) };
}

async function open(target: Parameters<typeof import('./MoveDialog').MoveDialog>[0]['target'], personId: string, spy: PlanningActions) {
  const { MoveDialog } = await import('./MoveDialog');

  return renderScreen(
    <MoveDialog plan={buildPlan(data)} personId={personId} target={target} from={today} to={today} actions={spy} onClose={() => undefined} />,
    { route: '/', path: '/', user: signedIn('Admin') },
  );
}

describe('MoveDialog', () => {
  it('names the site and the day, and assigns only when confirmed', async () => {
    const spy = actions();
    await open({ kind: 'site', projectId: HALL }, 'e2', spy);

    expect(await screen.findByText('Ivo Babić → Aldi Hall')).toBeDefined();
    expect(screen.getByText(/The Zidar skill is missing there/)).toBeDefined();
    expect(spy.assign).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Schedule' }));

    expect(spy.assign).toHaveBeenCalledWith('e2', HALL, today, today);
  });

  it('lets the period be lengthened before confirming', async () => {
    const spy = actions();
    await open({ kind: 'site', projectId: HALL }, 'e2', spy);

    await userEvent.click(await screen.findByRole('button', { name: 'Until the end of the week' }));
    await userEvent.click(screen.getByRole('button', { name: 'Schedule' }));

    expect(spy.assign).toHaveBeenCalledWith('e2', HALL, today, local(addDays(now, 2)));
  });

  it('moves a person to another site and says what the site they leave loses', async () => {
    const spy = actions();
    await open({ kind: 'site', projectId: BRIDGE }, 'e1', spy);

    expect(await screen.findByText(/Aldi Hall is left without the Zidar skill/)).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: 'Move' }));

    expect(spy.assign).toHaveBeenCalledWith('e1', BRIDGE, today, today);
  });

  it('frees a person', async () => {
    const spy = actions();
    await open({ kind: 'free' }, 'e1', spy);

    await userEvent.click(await screen.findByRole('button', { name: 'Make free' }));

    expect(spy.assign).toHaveBeenCalledWith('e1', null, today, today);
  });

  it('swaps two people over the stretch', async () => {
    const spy = actions();
    await open({ kind: 'person', personId: 'e3' }, 'e1', spy);

    expect(await screen.findByText('Swap Ana Novak and Marko Horvat?')).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: 'Swap' }));

    expect(spy.swap).toHaveBeenCalledWith('e1', 'e3', today, today);
  });

  it('will not swap two people who are on the same site', async () => {
    const spy = actions();
    await open({ kind: 'person', personId: 'e1' }, 'e1', spy);

    // Dropped on themselves it is the same person: the page ignores it, and the window shows nothing to swap.
    expect(await screen.findByText(/nothing to swap/)).toBeDefined();
    expect((screen.getByRole('button', { name: 'Swap' }) as HTMLButtonElement).disabled).toBe(true);
  });
});
