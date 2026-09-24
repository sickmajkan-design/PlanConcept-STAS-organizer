import { useQuery } from '@tanstack/react-query';

import { financeApi } from '../../api/finance';
import { useFinancePeriod } from './PeriodContext';

/**
 * The series for the board's shared period. Every widget that calls this with
 * the same period lands on the same query key, so two widgets are one request.
 */
export function useFinanceSeries() {
  const { period, granularity } = useFinancePeriod();

  return useQuery({
    queryKey: ['finance', 'series', period.from, period.to, granularity] as const,
    queryFn: () => financeApi.series({ from: period.from, to: period.to, granularity }),
    // Money changes when somebody records it; a few minutes stale is honest.
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });
}
