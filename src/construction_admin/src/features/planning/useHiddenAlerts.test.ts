/**
 * @vitest-environment jsdom
 */
import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { useHiddenAlerts } from './useHiddenAlerts';

beforeEach(() => window.localStorage.clear());

describe('useHiddenAlerts', () => {
  it('remembers a dismissal across a reload and brings it back on request', () => {
    const first = renderHook(() => useHiddenAlerts('u1', '2026-11-23'));
    act(() => first.result.current.hide('c|double|p1|a|b|2026-11-20|2026-11-30'));
    first.unmount();

    const second = renderHook(() => useHiddenAlerts('u1', '2026-11-23'));
    expect(second.result.current.hidden.has('c|double|p1|a|b|2026-11-20|2026-11-30')).toBe(true);

    act(() => second.result.current.restore());
    expect(second.result.current.hidden.size).toBe(0);
  });

  it('forgets a dismissal whose last day has passed, and keeps one account apart from another', () => {
    const { result } = renderHook(() => useHiddenAlerts('u1', '2026-11-23'));
    act(() => result.current.hide('a|p|s|AnnualLeave|2026-11-01|2026-11-10'));
    act(() => result.current.hide('a|p|s|AnnualLeave|2026-11-24|2026-11-26'));

    expect([...result.current.hidden]).toEqual(['a|p|s|AnnualLeave|2026-11-24|2026-11-26']);

    const other = renderHook(() => useHiddenAlerts('u2', '2026-11-23'));
    expect(other.result.current.hidden.size).toBe(0);
  });
});
