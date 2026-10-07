import { Chip, MenuItem, Paper, Select, Stack, Typography } from '@mui/material';
import { useState } from 'react';

import { SearchField } from '../../components/SearchField';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import type { ViewProps } from './planningTypes';

type Filter = 'all' | 'on' | 'free' | 'away';

/** One day, one row per person, with the site as a drop-down: the fastest way to tidy up. */
export function ListView({ plan, day, setSite, readOnly }: ViewProps & { day: number; setSite: (personId: string, projectId: string | null) => void }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const [filter, setFilter] = useState<Filter>('all');
  const [search, setSearch] = useState('');

  const date = plan.date(day);
  const options = plan.projects.filter((p) => (!p.startDate || date >= p.startDate) && (!p.endDate || date <= p.endDate));
  const work = plan.isWork[day];

  const rows = plan.people.filter((p) => {
    const cell = p.cells[day];
    if (filter === 'free' && (cell.project || cell.away)) return false;
    if (filter === 'away' && !cell.away) return false;
    if (filter === 'on' && (!cell.project || cell.away)) return false;
    const q = search.trim().toLowerCase();
    return !q || `${p.name} ${p.position}`.toLowerCase().includes(q);
  });

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <SearchField value={search} onChange={setSearch} placeholder={t('planning.t.search')} />
        {(['all', 'on', 'free', 'away'] as const).map((f) => (
          <Chip key={f} label={t(`planning.l.filter.${f}`)} color={filter === f ? 'primary' : 'default'} variant={filter === f ? 'filled' : 'outlined'} onClick={() => setFilter(f)} />
        ))}
      </Stack>

      <Paper data-tour="list" variant="outlined" sx={{ borderRadius: 2.5, overflowX: 'auto' }}>
        <table style={{ width: '100%', minWidth: 540, borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              {(['person', 'position', 'status', 'site'] as const).map((h) => (
                <th key={h} style={{ textAlign: 'left', padding: '10px 12px', fontSize: 12, fontWeight: 500, opacity: 0.7 }}>
                  {t(`planning.l.${h}`)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((p) => {
              const cell = p.cells[day];
              return (
                <tr key={p.id} style={{ borderTop: '1px solid rgba(128,128,128,.25)' }}>
                  <td style={{ padding: '6px 12px', fontWeight: 500 }}>{p.name}</td>
                  <td style={{ padding: '6px 12px' }}>
                    <Typography variant="body2" color="text.secondary">
                      {p.position}
                    </Typography>
                  </td>
                  <td style={{ padding: '6px 12px' }}>
                    {cell.away ? (
                      <Chip size="small" color="warning" label={enumLabel('absenceType', cell.away)} />
                    ) : cell.project ? (
                      t('planning.l.onSite')
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        {t('planning.l.free')}
                      </Typography>
                    )}
                  </td>
                  <td style={{ padding: '6px 12px' }}>
                    {cell.away || !work || readOnly ? (
                      '–'
                    ) : (
                      <Select
                        size="small"
                        displayEmpty
                        value={cell.project ?? ''}
                        onChange={(e) => setSite(p.id, e.target.value || null)}
                        sx={{ minWidth: 200 }}
                        inputProps={{ 'aria-label': p.name }}
                      >
                        <MenuItem value="">{t('planning.l.free')}</MenuItem>
                        {(cell.project && !options.some((s) => s.id === cell.project)
                          ? [...options, plan.projectById.get(cell.project)!].filter(Boolean)
                          : options
                        ).map((s) => (
                          <MenuItem key={s.id} value={s.id}>
                            {s.name}
                          </MenuItem>
                        ))}
                      </Select>
                    )}
                  </td>
                </tr>
              );
            })}
            {rows.length === 0 && (
              <tr>
                <td colSpan={4} style={{ padding: 16 }}>
                  {t('planning.l.none')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </Paper>
    </Stack>
  );
}
