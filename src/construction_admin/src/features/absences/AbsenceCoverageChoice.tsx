import { useEffect, useMemo, useState } from 'react';
import { Alert, FormControlLabel, Radio, RadioGroup, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { planningApi } from '../../api/planning';
import { buildPlan, candidates, workdayIndexes } from '../planning/planningLogic';
import { useT } from '../../i18n/useI18n';

/** The stand-in the office picked while granting a leave. Null when none is wanted. */
export interface CoverageChoice {
  employeeId: string;
  projectId: string;
  from: string;
  to: string;
  /** The stand-in is free only part of the time, so fill just those days. */
  onlyFreeDays: boolean;
}

/**
 * Asks, when somebody who is posted to a site goes on leave, whether somebody should stand in for the
 * days they are away, and suggests who: the same position, free the whole time first. Renders nothing
 * when the person is not posted anywhere over those days, or when the plan cannot be read.
 */
export function AbsenceCoverageChoice({
  employeeId,
  startDate,
  endDate,
  onChange,
}: {
  employeeId: string | undefined;
  startDate: string | undefined;
  endDate: string | undefined;
  onChange: (choice: CoverageChoice | null) => void;
}) {
  const t = useT();
  const enabled = !!employeeId && !!startDate && !!endDate && endDate >= startDate;

  // No business-unit filter: the person is on leave whichever unit the header happens to show.
  const query = useQuery({
    queryKey: ['absenceCoverage', startDate, endDate],
    queryFn: () => planningApi.get({ from: startDate!, to: endDate! }),
    enabled,
    retry: false,
  });

  const [pick, setPick] = useState('');

  const found = useMemo(() => {
    if (!query.data?.employees || !employeeId) return null;

    const plan = buildPlan(query.data);
    const person = plan.people.find((p) => p.id === employeeId);
    if (!person) return null;

    const days = workdayIndexes(plan, 0, plan.days - 1);
    const counts = new Map<string, number>();
    for (const i of days) {
      const project = person.cells[i].project;
      if (project) counts.set(project, (counts.get(project) ?? 0) + 1);
    }

    const top = [...counts.entries()].sort((a, b) => b[1] - a[1])[0];
    if (!top) return null;

    const siteDays = days.filter((i) => person.cells[i].project === top[0]);
    return {
      plan,
      person,
      projectId: top[0],
      siteName: plan.projectById.get(top[0])?.name ?? '',
      siteDays,
      list: candidates(plan, employeeId, top[0], siteDays).slice(0, 8),
    };
  }, [query.data, employeeId]);

  useEffect(() => {
    setPick('');
  }, [employeeId, startDate, endDate]);

  useEffect(() => {
    if (!found || !pick) {
      onChange(null);
      return;
    }

    const chosen = found.list.find((c) => c.person.id === pick);
    onChange(
      chosen
        ? {
            employeeId: pick,
            projectId: found.projectId,
            from: found.plan.date(found.siteDays[0]),
            to: found.plan.date(found.siteDays[found.siteDays.length - 1]),
            onlyFreeDays: chosen.tier === 2,
          }
        : null,
    );
  }, [found, pick, onChange]);

  if (!found) return null;

  return (
    <Stack spacing={0.5}>
      <Alert severity="info">
        {t('planning.cover.intro', { name: found.person.name, site: found.siteName, count: found.siteDays.length })}
      </Alert>
      <RadioGroup value={pick} onChange={(event) => setPick(event.target.value)}>
        <FormControlLabel value="" control={<Radio size="small" />} label={t('planning.cover.none')} />
        {found.list.map((c) => (
          <FormControlLabel
            key={c.person.id}
            value={c.person.id}
            control={<Radio size="small" />}
            label={
              <Typography variant="body2">
                {c.person.name} · {c.person.position}{' '}
                <Typography component="span" variant="caption" color="text.secondary">
                  {c.kind === 'free'
                    ? t('planning.cand.free')
                    : c.kind === 'partial'
                      ? t('planning.cand.partial', { free: c.freeDays, total: c.totalDays })
                      : c.kind === 'surplus'
                        ? t('planning.cand.surplusAt', { site: found.plan.projectById.get(c.fromProjectId ?? '')?.name ?? '' })
                        : t('planning.cand.otherSkill')}
                </Typography>
              </Typography>
            }
          />
        ))}
      </RadioGroup>
    </Stack>
  );
}
