import { EditOutlined, ExpandMoreOutlined, ChevronRightOutlined } from '@mui/icons-material';
import { Box, ButtonBase, Chip, IconButton, Paper, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useMemo, useState } from 'react';

import {
  columnsFor,
  covered,
  freeCount,
  missing,
  needOf,
  positionKey,
  roleCount,
  weekday,
  workIndexesIn,
  type Column,
} from '../../features/planning/planningLogic';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { shortDate, siteColor, WEEKDAYS } from './planningUi';
import type { ViewProps } from './planningTypes';

type Tone = 'ok' | 'warn' | 'bad';

const toneSx = (tone: Tone) => {
  const color = tone === 'ok' ? '#2f9e44' : tone === 'warn' ? '#d9822b' : '#d6336c';
  return { bgcolor: alpha(color, 0.16), color };
};

/** Each project against what it needs, by period, with the shortage per skill underneath. */
export function NeedView({
  plan,
  range,
  openSite,
  editNeeds,
  readOnly,
}: ViewProps & { range: { from: string; to: string } }) {
  const t = useT();
  const [open, setOpen] = useState<Record<string, boolean>>({});

  const columns = useMemo(() => columnsFor(range.from, range.to), [range.from, range.to]);
  const perDay = columns.length > 0 && columns[0].from === columns[0].to;
  const columnWidth = perDay ? 34 : 78;

  const daysOf = (c: Column) =>
    workIndexesIn(plan, c);

  const rangeDays = useMemo(() => columns.flatMap((c) => daysOf(c)), [columns, plan]); // eslint-disable-line react-hooks/exhaustive-deps

  const skills = useMemo(() => {
    const seen = new Map<string, string>();
    for (const p of plan.projects) for (const n of p.needs) if (!seen.has(positionKey(n.position))) seen.set(positionKey(n.position), n.position);
    return [...seen.values()].sort((a, b) => a.localeCompare(b));
  }, [plan.projects]);

  const cell = (projectId: string, column: Column, position?: string, count?: number) => {
    const site = plan.projectById.get(projectId)!;
    const days = daysOf(column).filter((i) => {
      const d = plan.date(i);
      return (!site.startDate || d >= site.startDate) && (!site.endDate || d <= site.endDate);
    });

    if (workIndexesIn(plan, column).length === 0) return <Box key={column.from} component="td" sx={{ bgcolor: 'action.hover' }} />;
    if (days.length === 0) return <td key={column.from} />;

    const need = position ? count! : needOf(site);
    const have = Math.min(...days.map((i) => (position ? roleCount(plan, projectId, i, position) : covered(plan, site, i))));
    const tone: Tone = have >= need ? 'ok' : have === 0 ? 'bad' : 'warn';

    return (
      <td key={column.from}>
        <ButtonBase
          onClick={() => openSite(projectId, plan.date(days[0]), plan.date(days[days.length - 1]), position)}
          disabled={readOnly}
          sx={{ width: '100%', borderRadius: 1.5, py: 0.9, fontSize: 12, fontWeight: 600, fontVariantNumeric: 'tabular-nums', ...toneSx(tone) }}
        >
          {have}/{need}
        </ButtonBase>
      </td>
    );
  };

  const footer = (pick: (days: number[]) => number, bad: boolean) =>
    columns.map((c) => {
      const days = daysOf(c);
      if (days.length === 0) return <Box key={c.from} component="td" sx={{ bgcolor: 'action.hover' }} />;
      const value = pick(days);
      return (
        <Box key={c.from} component="td" sx={{ textAlign: 'center', fontSize: 12, fontVariantNumeric: 'tabular-nums', fontWeight: value ? 700 : 400, color: value ? (bad ? 'error.main' : 'success.main') : 'text.secondary' }}>
          {value || '–'}
        </Box>
      );
    });

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: '75ch' }}>
        {t('planning.n.note')}
      </Typography>

      <Paper data-tour="need-table" variant="outlined" sx={{ borderRadius: 2.5, overflowX: 'auto' }}>
        <Box
          component="table"
          sx={{
            borderCollapse: 'separate',
            borderSpacing: 0,
            tableLayout: 'fixed',
            width: '100%',
            minWidth: 230 + columns.length * columnWidth,
            '& td, & th': { borderBottom: 1, borderColor: 'divider', p: '4px 3px', textAlign: 'center' },
            '& th': { fontWeight: 500, fontSize: 11, color: 'text.secondary', lineHeight: 1.2, bgcolor: 'background.paper' },
            '& td:first-of-type, & th:first-of-type': { position: 'sticky', left: 0, zIndex: 1, bgcolor: 'background.paper', textAlign: 'left', pl: 1.5, minWidth: 230, borderRight: 1, borderRightColor: 'divider' },
          }}
        >
          <colgroup>
            <col style={{ width: 230 }} />
            {columns.map((c) => (
              <col key={c.from} />
            ))}
          </colgroup>
          <thead>
            <tr>
              <th>{t('planning.n.project')}</th>
              {columns.map((c) => (
                <Box key={c.from} component="th" sx={{ bgcolor: perDay && plan.indexOf(c.from) >= 0 && !plan.isWork[plan.indexOf(c.from)] ? 'action.hover !important' : undefined }}>
                  {perDay ? (
                    <>
                      {WEEKDAYS[weekday(c.from)][0]}
                      <br />
                      {Number(c.from.slice(8))}
                    </>
                  ) : (
                    <>
                      {shortDate(c.from)}
                      <br />–{shortDate(c.to)}
                    </>
                  )}
                </Box>
              ))}
            </tr>
          </thead>
          <tbody>
            {plan.projects.map((site) => {
              const expanded = !!open[site.id];

              return [
                <tr key={site.id}>
                  <td>
                    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                      <IconButton size="small" onClick={() => setOpen({ ...open, [site.id]: !expanded })} aria-expanded={expanded} aria-label={t('planning.n.expand')} disabled={site.needs.length === 0}>
                        {expanded ? <ExpandMoreOutlined fontSize="small" /> : <ChevronRightOutlined fontSize="small" />}
                      </IconButton>
                      <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: siteColor(site.id), flex: 'none' }} />
                      <Box sx={{ minWidth: 0, flex: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                          {site.name}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                          {site.startDate ? formatDate(site.startDate) : '…'} – {site.endDate ? formatDate(site.endDate) : '…'} ·{' '}
                          {site.needs.length ? t('planning.n.needs', { count: needOf(site) }) : t('planning.n.noNeeds')}
                        </Typography>
                      </Box>
                      {/* What a project needs is set by a project manager and above, even where moving people is allowed. */}
                      {!readOnly && !plan.isScoped && (
                        <Tooltip title={t('planning.n.editNeeds')}>
                          <IconButton size="small" onClick={() => editNeeds(site.id)} aria-label={t('planning.n.editNeeds')}>
                            <EditOutlined fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                    </Stack>
                  </td>
                  {columns.map((c) => (site.needs.length ? cell(site.id, c) : <td key={c.from} />))}
                </tr>,
                ...(expanded
                  ? site.needs.map((n) => (
                      <tr key={`${site.id}-${n.position}`}>
                        <td>
                          <Typography variant="caption" color="text.secondary" sx={{ pl: 6 }}>
                            {n.position} · {t('planning.n.needs', { count: n.count })}
                          </Typography>
                        </td>
                        {columns.map((c) => cell(site.id, c, n.position, n.count))}
                      </tr>
                    ))
                  : []),
              ];
            })}
            <Box component="tr" sx={{ '& td': { bgcolor: 'action.hover', borderBottom: 0 } }}>
              <td>{t('planning.n.footMissing')}</td>
              {footer((days) => Math.max(...days.map((i) => missing(plan, i))), true)}
            </Box>
            <Box component="tr" sx={{ '& td': { bgcolor: 'action.hover', borderBottom: 0 } }}>
              <td>{t('planning.n.footFree')}</td>
              {footer((days) => Math.min(...days.map((i) => freeCount(plan, i))), false)}
            </Box>
          </tbody>
        </Box>
      </Paper>

      {skills.length > 0 && (
        <Box data-tour="skills">
          <Typography variant="h6" sx={{ mb: 1 }}>
            {t('planning.n.bySkill')}
          </Typography>
          <Paper variant="outlined" sx={{ borderRadius: 2.5, overflowX: 'auto' }}>
            <Box component="table" sx={{ width: '100%', minWidth: 480, borderCollapse: 'collapse', '& td, & th': { p: 1.25, borderBottom: 1, borderColor: 'divider', textAlign: 'left' }, '& th': { fontSize: 12, color: 'text.secondary', fontWeight: 500 }, '& tr:last-of-type td': { borderBottom: 0 } }}>
              <thead>
                <tr>
                  <th>{t('planning.n.skill')}</th>
                  <th>{t('planning.n.maxShortage')}</th>
                  <th>{t('planning.n.minFree')}</th>
                  <th>{t('planning.n.state')}</th>
                </tr>
              </thead>
              <tbody>
                {skills.map((skill) => {
                  const shortage = Math.max(0, ...rangeDays.map((i) => missing(plan, i, skill)));
                  const free = rangeDays.length ? Math.min(...rangeDays.map((i) => freeCount(plan, i, skill))) : 0;
                  const gap = Math.max(0, ...rangeDays.map((i) => missing(plan, i, skill) - freeCount(plan, i, skill)));

                  return (
                    <tr key={skill}>
                      <td>
                        <b>{skill}</b>
                      </td>
                      <td>{shortage || '–'}</td>
                      <td>{rangeDays.length ? free : '–'}</td>
                      <td>
                        {shortage === 0 ? (
                          <Chip size="small" color="success" label={t('planning.n.covered')} />
                        ) : gap === 0 ? (
                          <Chip size="small" color="warning" label={t('planning.n.fillable')} />
                        ) : (
                          <Chip size="small" color="error" label={t('planning.n.hire', { count: gap })} />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </Box>
          </Paper>
        </Box>
      )}
    </Stack>
  );
}
