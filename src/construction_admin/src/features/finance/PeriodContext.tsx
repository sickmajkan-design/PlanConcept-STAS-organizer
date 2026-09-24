import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { useAuth } from '../../auth/useAuth';
import {
  defaultGranularity,
  periodFromPreset,
  periodPresets,
  validateCustomRange,
  type Period,
  type PeriodPreset,
} from './periods';
import type { FinanceGranularity } from '../../api/finance';

interface PeriodContextValue {
  period: Period;
  granularity: FinanceGranularity;
  setPreset: (preset: Exclude<PeriodPreset, 'custom'>) => void;
  /** Ignored unless the range is usable — see `validateCustomRange`. */
  setCustom: (from: string, to: string) => void;
  reset: () => void;
  isDefault: boolean;
}

const DEFAULT_PRESET: PeriodPreset = 'thisMonth';

const PeriodContext = createContext<PeriodContextValue | null>(null);

function storageKey(userId: string | undefined): string {
  return `finance.period.${userId ?? 'anonymous'}`;
}

/**
 * Only the choice is stored, not the dates behind a preset — "this month"
 * has to mean this month tomorrow, not the month it was picked in. A custom
 * range is the exception, since its dates are the choice.
 */
function load(userId: string | undefined): Period {
  try {
    const raw = window.localStorage.getItem(storageKey(userId));
    if (raw) {
      const saved = JSON.parse(raw) as { preset?: string; from?: string; to?: string };
      if (saved.preset && (periodPresets as readonly string[]).includes(saved.preset)) {
        const preset = saved.preset as PeriodPreset;
        if (preset !== 'custom') return periodFromPreset(preset);
        if (saved.from && saved.to && validateCustomRange(saved.from, saved.to) === null) {
          return periodFromPreset('custom', { from: saved.from, to: saved.to });
        }
      }
    }
  } catch {
    // Storage can be blocked or hold something else; the default is fine.
  }
  return periodFromPreset(DEFAULT_PRESET);
}

function save(userId: string | undefined, period: Period) {
  try {
    window.localStorage.setItem(
      storageKey(userId),
      JSON.stringify(period.preset === 'custom' ? period : { preset: period.preset }),
    );
  } catch {
    // Nothing to do: the choice just will not survive a reload.
  }
}

/**
 * The one period every finance widget on the board reads, so changing it once
 * changes them all. Remembered per account, not per widget.
 */
export function FinancePeriodProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const userId = user?.id;
  const [period, setPeriod] = useState<Period>(() => load(userId));

  const apply = useCallback(
    (next: Period) => {
      setPeriod(next);
      save(userId, next);
    },
    [userId],
  );

  const value = useMemo<PeriodContextValue>(
    () => ({
      period,
      granularity: defaultGranularity(period),
      setPreset: (preset) => apply(periodFromPreset(preset)),
      setCustom: (from, to) => {
        if (validateCustomRange(from, to) === null) apply(periodFromPreset('custom', { from, to }));
      },
      reset: () => apply(periodFromPreset(DEFAULT_PRESET)),
      isDefault: period.preset === DEFAULT_PRESET,
    }),
    [period, apply],
  );

  return <PeriodContext.Provider value={value}>{children}</PeriodContext.Provider>;
}

export function useFinancePeriod(): PeriodContextValue {
  const value = useContext(PeriodContext);
  if (!value) throw new Error('useFinancePeriod must be used inside FinancePeriodProvider');
  return value;
}
