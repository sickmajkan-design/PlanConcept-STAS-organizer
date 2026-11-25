// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from 'vitest';

import { loadOps, saveOps, viewShift, type OutboxOp } from './shiftOutbox';

const none = () => null;
const op = (kind: 'in' | 'out', at: string, projectId?: string): OutboxOp => ({ id: `${kind}-${at}`, kind, occurredAt: at, input: kind === 'in' ? { projectId } : { breakMinutes: 0 } });

describe('shift outbox', () => {
  beforeEach(() => localStorage.clear());

  it('shows the server shift untouched when nothing waits', () => {
    expect(viewShift({ startedAt: 'A', projectName: 'Site' }, [], none)).toEqual({ startedAt: 'A', projectName: 'Site', pending: false });
    expect(viewShift(null, [], none)).toBeNull();
  });

  it('a start that waits shows as a running shift from the moment of the press', () => {
    expect(viewShift(null, [op('in', 'T1', 'p1')], (id) => (id === 'p1' ? 'Site 1' : null))).toEqual({ startedAt: 'T1', projectName: 'Site 1', pending: true });
  });

  it('an end that waits closes the shift it follows', () => {
    expect(viewShift({ startedAt: 'A', projectName: null }, [op('out', 'T2')], none)).toBeNull();
    expect(viewShift(null, [op('in', 'T1'), op('out', 'T2')], none)).toBeNull();
  });

  it('applies the actions in order', () => {
    expect(viewShift(null, [op('in', 'T1'), op('out', 'T2'), op('in', 'T3')], none)?.startedAt).toBe('T3');
  });

  it('keeps what was saved and survives damaged storage', () => {
    saveOps('u1', [op('in', 'T1')]);
    expect(loadOps('u1')).toHaveLength(1);
    expect(loadOps('u2')).toEqual([]);

    localStorage.setItem('shiftOutbox.v1.u3', '{not json');
    expect(loadOps('u3')).toEqual([]);

    localStorage.setItem('shiftOutbox.v1.u4', JSON.stringify([{ kind: 'in' }, { id: 'x', kind: 'out', occurredAt: 'T' }]));
    expect(loadOps('u4')).toHaveLength(1);
  });
});
