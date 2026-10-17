import { ExpandMoreOutlined, FilterListOutlined } from '@mui/icons-material';
import { Badge, Box, Button, ButtonBase, FormControlLabel, MenuItem, Paper, Popover, Radio, RadioGroup, Stack, TextField, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useMemo, useState } from 'react';

import { SearchField } from '../../components/SearchField';
import {
  columnsFor,
  freeCount,
  missing,
  positionKey,
  weekday,
  workIndexesIn,
  type Column,
  type Plan,
  type PlanPerson,
} from '../../features/planning/planningLogic';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { AWAY_COLOR, awayChipSx, shortDate, siteColor, WEEKDAYS } from './planningUi';
import type { ViewProps } from './planningTypes';

type Filter = 'all' | 'free' | 'away';

interface Run {
  /** `p:<projectId>`, `away:<type>`, `free`, `mixed`, or `weekend`. */
  value: string;
  columns: number;
  from: string;
  to: string;
}

function valueOf(plan: Plan, person: PlanPerson, column: Column): string {
  const days = workIndexesIn(plan, column);

  if (days.length === 0) return 'weekend';

  const set = new Set(
    days.map((i) => {
      const cell = person.cells[i];
      return cell.away ? `away:${cell.away}` : cell.project ? `p:${cell.project}` : 'free';
    }),
  );

  return set.size === 1 ? [...set][0] : 'mixed';
}

function runsOf(plan: Plan, person: PlanPerson, columns: Column[]): Run[] {
  const out: Run[] = [];
  const values = columns.map((c) => valueOf(plan, person, c));

  // A weekend between two identical stretches belongs to both, so the bar runs on through it.
  columns.forEach((_, i) => {
    if (values[i] !== 'weekend') return;
    const before = values.slice(0, i).reverse().find((v) => v !== 'weekend');
    const after = values.slice(i + 1).find((v) => v !== 'weekend');
    if (before !== undefined && before === after && before !== 'mixed') values[i] = before;
  });

  for (const [i, column] of columns.entries()) {
    const value = values[i];
    const tail = out[out.length - 1];

    if (tail && tail.value === value && value !== 'mixed' && value !== 'weekend') {
      tail.columns++;
      tail.to = column.to;
    } else {
      out.push({ value, columns: 1, from: column.from, to: column.to });
    }
  }

  return out;
}

/** People as rows, time as columns: a bar for every stretch on a site, hatched where they are away. */
export function TimelineView({
  plan,
  range,
  openWorker,
  readOnly,
}: ViewProps & { range: { from: string; to: string } }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [search, setSearch] = useState('');
  const [skill, setSkill] = useState('');
  const [filter, setFilter] = useState<Filter>('all');
  const [filterAnchor, setFilterAnchor] = useState<HTMLElement | null>(null);
  const [legendAnchor, setLegendAnchor] = useState<HTMLElement | null>(null);

  const columns = useMemo(() => columnsFor(range.from, range.to), [range.from, range.to]);
  const perDay = columns.length > 0 && columns[0].from === columns[0].to;
  const columnWidth = perDay ? 34 : 78;

  const rangeDays = useMemo(
    () =>
      columns.flatMap((c) => workIndexesIn(plan, c)),
    [columns, plan],
  );

  const people = plan.people.filter((p) => {
    if (skill && p.key !== positionKey(skill)) return false;
    if (filter === 'free' && !rangeDays.some((i) => !p.cells[i].project && !p.cells[i].away)) return false;
    if (filter === 'away' && !rangeDays.some((i) => p.cells[i].away)) return false;
    const q = search.trim().toLowerCase();
    return !q || `${p.name} ${p.position}`.toLowerCase().includes(q);
  });

  const skills = useMemo(() => {
    const seen = new Map<string, string>();
    for (const p of plan.people) if (!seen.has(p.key)) seen.set(p.key, p.position);
    return [...seen.values()].sort((a, b) => a.localeCompare(b));
  }, [plan.people]);

  const footer = (pick: (days: number[]) => number, tone: 'good' | 'bad') =>
    columns.map((c) => {
      const days = workIndexesIn(plan, c);

      if (days.length === 0) return <Box key={c.from} component="td" sx={{ bgcolor: 'action.hover' }} />;

      const value = pick(days);

      return (
        <Box
          key={c.from}
          component="td"
          sx={{ textAlign: 'center', fontVariantNumeric: 'tabular-nums', fontSize: 12, color: value ? (tone === 'bad' ? 'error.main' : 'success.main') : 'text.secondary', fontWeight: value ? 700 : 400 }}
        >
          {value || '–'}
        </Box>
      );
    });

  const siteNames = plan.projectById;

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <SearchField value={search} onChange={setSearch} placeholder={t('planning.t.search')} />
        <TextField select size="small" value={skill} onChange={(e) => setSkill(e.target.value)} sx={{ minWidth: 170 }} aria-label={t('planning.t.skill')} slotProps={{ select: { displayEmpty: true } }}>
          <MenuItem value="">{t('planning.t.allSkills')}</MenuItem>
          {skills.map((s) => (
            <MenuItem key={s} value={s}>
              {s}
            </MenuItem>
          ))}
        </TextField>
        {/* Who to show: everyone, those with free days, those away. One button with a mark when it is not "everyone",
            instead of three chips beside the search. */}
        <Badge color="primary" variant="dot" invisible={filter === 'all'} overlap="rectangular">
          <Button
            size="small"
            variant="outlined"
            color="primary"
            startIcon={<FilterListOutlined fontSize="small" />}
            endIcon={<ExpandMoreOutlined fontSize="small" />}
            onClick={(event) => setFilterAnchor(event.currentTarget)}
            sx={{ whiteSpace: 'nowrap' }}
          >
            {filter === 'all' ? t('planning.t.filters') : t(`planning.t.filter.${filter}`)}
          </Button>
        </Badge>
        <Popover
          open={!!filterAnchor}
          anchorEl={filterAnchor}
          onClose={() => setFilterAnchor(null)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        >
          <RadioGroup
            value={filter}
            onChange={(event) => {
              setFilter(event.target.value as Filter);
              setFilterAnchor(null);
            }}
            sx={{ p: 1.5, pr: 2.5 }}
          >
            {(['all', 'free', 'away'] as const).map((f) => (
              <FormControlLabel key={f} value={f} control={<Radio size="small" />} label={t(`planning.t.filter.${f}`)} />
            ))}
          </RadioGroup>
        </Popover>
      </Stack>

      <Legend
        entries={[
          ...plan.projects.map((p) => ({ key: p.id, color: siteColor(p.id), name: p.name })),
          { key: 'away', color: AWAY_COLOR, name: t('planning.t.legendAway') },
        ]}
        anchor={legendAnchor}
        setAnchor={setLegendAnchor}
        moreLabel={(count) => t('planning.t.legendMore', { count })}
      />

      <Paper data-tour="timeline" variant="outlined" sx={{ borderRadius: 2.5, overflowX: 'auto' }}>
        <Box
          component="table"
          sx={{
            borderCollapse: 'separate',
            borderSpacing: 0,
            tableLayout: 'fixed',
            width: '100%',
            minWidth: 170 + columns.length * columnWidth,
            '& td, & th': { borderBottom: 1, borderColor: 'divider', p: '4px 3px' },
            '& th': { fontWeight: 500, fontSize: 11, color: 'text.secondary', lineHeight: 1.2, bgcolor: 'background.paper' },
            '& td:first-of-type, & th:first-of-type': { position: 'sticky', left: 0, zIndex: 1, bgcolor: 'background.paper', textAlign: 'left', pl: 1.5, minWidth: 170, borderRight: 1, borderRightColor: 'divider' },
          }}
        >
          <colgroup>
            <col style={{ width: 170 }} />
            {columns.map((c) => (
              <col key={c.from} />
            ))}
          </colgroup>
          <thead>
            <tr>
              <th>{t('planning.t.person')}</th>
              {columns.map((c) => {
                const isToday = c.from <= plan.today && c.to >= plan.today;
                const idx = plan.indexOf(c.from);
                const weekend = perDay && idx >= 0 && !plan.isWork[idx];
                const holiday = perDay ? plan.holidayNames.get(c.from) : undefined;
                return (
                  <Box
                    key={c.from}
                    component="th"
                    title={holiday}
                    sx={{ textDecoration: holiday ? 'underline dotted' : undefined, bgcolor: weekend ? 'action.hover !important' : undefined, boxShadow: isToday ? (theme: { palette: { primary: { main: string } } }) => `inset 0 -2px 0 ${theme.palette.primary.main}` : undefined, color: isToday ? 'text.primary !important' : undefined }}
                  >
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
                );
              })}
            </tr>
          </thead>
          <tbody>
            {people.map((person) => (
              <tr key={person.id}>
                <Box component="td">
                  <Typography variant="body2" sx={{ fontWeight: 500 }} noWrap>
                    {person.name}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    {person.position}
                  </Typography>
                </Box>
                {runsOf(plan, person, columns).map((run) => {
                  if (run.value === 'weekend') return <Box key={run.from} component="td" sx={{ bgcolor: 'action.hover' }} />;

                  let label = '';
                  let sx: object = {};
                  const long = run.columns > 1 || !perDay;

                  if (run.value === 'free') {
                    sx = { border: '1px dashed', borderColor: 'divider', color: 'text.secondary', fontWeight: 400 };
                    label = long ? t('planning.t.free') : '';
                  } else if (run.value === 'mixed') {
                    sx = { border: '1px solid', borderColor: 'divider', color: 'text.secondary', fontWeight: 500 };
                    label = t('planning.t.mixed');
                  } else if (run.value.startsWith('away:')) {
                    sx = awayChipSx;
                    const type = run.value.slice(5);
                    label = long ? enumLabel('absenceType', type) : type === 'SickLeave' ? 'BO' : 'GO';
                  } else {
                    const id = run.value.slice(2);
                    const name = siteNames.get(id)?.name ?? '?';
                    sx = { bgcolor: alpha(siteColor(id), 0.16), color: siteColor(id) };
                    label = long ? name : name.slice(0, 2).toUpperCase();
                  }

                  return (
                    <Box key={run.from} component="td" colSpan={run.columns} sx={{ p: '4px 2px !important' }}>
                      <ButtonBase
                        onClick={() => openWorker(person.id, run.from, run.to)}
                        disabled={readOnly}
                        sx={{ width: '100%', borderRadius: 1.5, p: '5px 4px', fontSize: 12, fontWeight: 600, display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', '&:hover': { outline: '1px solid', outlineColor: 'text.secondary' }, ...sx }}
                      >
                        {label}
                      </ButtonBase>
                    </Box>
                  );
                })}
              </tr>
            ))}
            <Box component="tr" sx={{ '& td': { bgcolor: 'action.hover', borderBottom: 0 } }}>
              <td>{t('planning.t.footFree', { skill: skill ? `: ${skill}` : '' })}</td>
              {footer((days) => Math.min(...days.map((i) => freeCount(plan, i, skill || undefined))), 'good')}
            </Box>
            <Box component="tr" sx={{ '& td': { bgcolor: 'action.hover', borderBottom: 0 } }}>
              <td>{t('planning.t.footMissing', { skill: skill ? `: ${skill}` : '' })}</td>
              {footer((days) => Math.max(...days.map((i) => missing(plan, i, skill || undefined))), 'bad')}
            </Box>
          </tbody>
        </Box>
      </Paper>
    </Stack>
  );
}

const LEGEND_SHOWN = 4;
const LEGEND_NAME_MAX = 30;

const short = (name: string) => (name.length > LEGEND_NAME_MAX ? `${name.slice(0, LEGEND_NAME_MAX - 1)}…` : name);

/**
 * What the colours mean: the first few sites on one line with their names cut short (the whole name in a
 * tooltip), and "+ N more" opening the rest. A list of long site names used to take three lines of its own above
 * the table.
 */
function Legend({
  entries,
  anchor,
  setAnchor,
  moreLabel,
}: {
  entries: { key: string; color: string; name: string }[];
  anchor: HTMLElement | null;
  setAnchor: (element: HTMLElement | null) => void;
  moreLabel: (count: number) => string;
}) {
  // The away entry always stays on the line; the sites share what room is left.
  const away = entries[entries.length - 1];
  const sites = entries.slice(0, -1);
  const shown = sites.slice(0, LEGEND_SHOWN);
  const rest = sites.slice(LEGEND_SHOWN);

  const item = (entry: { key: string; color: string; name: string }, cut: boolean) => (
    <Box key={entry.key} title={entry.name} sx={{ display: 'flex', alignItems: 'center', gap: 0.75, flex: 'none', whiteSpace: 'nowrap' }}>
      <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: entry.color, flex: 'none' }} />
      {cut ? short(entry.name) : entry.name}
    </Box>
  );

  return (
    <Stack direction="row" spacing={2} useFlexGap sx={{ fontSize: 12, color: 'text.secondary', alignItems: 'center', overflow: 'hidden' }}>
      {shown.map((entry) => item(entry, true))}
      {rest.length > 0 && (
        <>
          <ButtonBase onClick={(event) => setAnchor(event.currentTarget)} sx={{ fontSize: 12, color: 'inherit', whiteSpace: 'nowrap', borderRadius: 1, px: 0.5 }}>
            {moreLabel(rest.length)}
            <ExpandMoreOutlined sx={{ fontSize: 16 }} />
          </ButtonBase>
          <Popover open={!!anchor} anchorEl={anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}>
            <Stack spacing={0.75} sx={{ p: 1.5, maxWidth: 420, fontSize: 13 }}>
              {rest.map((entry) => (
                <Box key={entry.key} sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                  <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: entry.color, flex: 'none' }} />
                  {entry.name}
                </Box>
              ))}
            </Stack>
          </Popover>
        </>
      )}
      {item(away, false)}
    </Stack>
  );
}
