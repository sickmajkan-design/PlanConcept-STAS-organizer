import { Box, Paper, Popper, Stack, Typography } from '@mui/material';
import { alpha, useTheme, type Theme } from '@mui/material/styles';
import { memo, useCallback, useEffect, useRef, useState, type FocusEvent, type MouseEvent, type ReactNode } from 'react';

import { EQUIPMENT_SVG, EquipmentIcon } from '../../components/EquipmentIcon';
import {
  groupCounts,
  isMismatch,
  type EquipmentGroup,
  type EquipmentAlert,
  type EquipmentItem,
  type SiteEquipment,
} from '../../features/planning/equipment';
import type { Plan, PlanPerson } from '../../features/planning/planningLogic';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';

export const EQ_WARN = '#e07b00';

/** Amber text: dark brown on light, a light amber on dark. */
const warnText = (theme: Theme): string => (theme.palette.mode === 'dark' ? theme.palette.warning.light : '#8a4a00');

/** The handlers that open the shared card on an element; none at all while the board is busy. */
export interface HoverBind {
  onMouseEnter?: (event: MouseEvent<HTMLElement>) => void;
  onMouseLeave?: () => void;
  onFocus?: (event: FocusEvent<HTMLElement>) => void;
  onBlur?: () => void;
}

export type BindCard = (content: () => ReactNode) => HoverBind;

/**
 * One hover card for the whole board. Every icon asks this single card to open on itself, so a board of two hundred
 * people holds one popup, not eight hundred, and a card's content is only built at the moment somebody points at an icon.
 */
export function useHoverCard(paused = false): { bind: BindCard; element: ReactNode } {
  const [card, setCard] = useState<{ anchor: HTMLElement; content: ReactNode } | null>(null);
  const timer = useRef<number | undefined>(undefined);
  // Read through a ref so that carrying somebody does not change `bind`, and with it the props of every icon on the board.
  const pausedRef = useRef(paused);
  pausedRef.current = paused;

  const cancel = useCallback(() => window.clearTimeout(timer.current), []);
  const show = useCallback(
    (anchor: HTMLElement, content: ReactNode) => {
      if (pausedRef.current) return;
      cancel();
      setCard({ anchor, content });
    },
    [cancel],
  );
  const hide = useCallback(() => {
    cancel();
    timer.current = window.setTimeout(() => setCard(null), 140);
  }, [cancel]);

  useEffect(() => () => window.clearTimeout(timer.current), []);

  // Nothing opens while somebody is being carried across the board.
  const bind: BindCard = useCallback(
    (content) => ({
      onMouseEnter: (event) => show(event.currentTarget, content()),
      onMouseLeave: hide,
      onFocus: (event) => show(event.currentTarget, content()),
      onBlur: hide,
    }),
    [show, hide],
  );

  useEffect(() => {
    if (paused) setCard(null);
  }, [paused]);

  const element = card ? (
    <Popper open anchorEl={card.anchor} placement="bottom-start" sx={{ zIndex: 1400 }}>
      <Paper
        role="tooltip"
        elevation={8}
        onMouseEnter={cancel}
        onMouseLeave={hide}
        sx={{ mt: 1, p: 1.75, width: 'min(360px, 92vw)', borderRadius: 3, border: 1, borderColor: 'divider' }}
      >
        {card.content}
      </Paper>
    </Popper>
  ) : null;

  return { bind, element };
}

const groupKey = (group: string) => `planning.eq.${group}` as MessageKey;

const CLUSTERS = new Map<string, { image: string; width: number }>();

/**
 * Everything a person's equipment cluster shows (the icons, the counts, the warning dot) as ONE cached image. A board of
 * two hundred people then costs two hundred elements instead of about fifteen hundred, which is what keeps dragging a
 * worker across it smooth.
 */
function clusterImage(groups: { group: EquipmentGroup; count: number }[], warn: boolean, color: string): { image: string; width: number } {
  const key = `${groups.map((g) => `${g.group}${g.count}`).join(',')}|${warn ? 1 : 0}|${color}`;
  const hit = CLUSTERS.get(key);
  if (hit) return hit;

  let x = 6;
  let body = '';

  for (const g of groups) {
    body += `<g transform="translate(${x} 3) scale(0.8333)" fill="${color}">${EQUIPMENT_SVG[g.group]}</g>`;
    x += 20;

    if (g.count > 1) {
      const digits = String(g.count).length;
      body += `<text x="${x + 1}" y="17.5" font-size="11" font-weight="700" font-family="sans-serif" fill="${color}">${g.count}</text>`;
      x += 1 + digits * 6.5;
    }

    x += 4;
  }

  if (warn) {
    body += `<circle cx="${x + 3.5}" cy="13" r="3.5" fill="${EQ_WARN}"/>`;
    x += 11;
  }

  const width = x + 2;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="26" viewBox="0 0 ${width} 26">${body}</svg>`;
  const result = { image: `url("data:image/svg+xml,${encodeURIComponent(svg)}")`, width };
  CLUSTERS.set(key, result);

  return result;
}

/**
 * The icons of what a person holds, one per kind with a count, and a dot when something about it needs a look. One
 * element, memoised on plain values: dragging a worker across the board (which redraws every row) leaves it alone.
 */
export const EquipmentCluster = memo(function EquipmentCluster({
  plan,
  person,
  day,
  items,
  mismatch,
  idle,
  bind,
}: {
  plan: Plan;
  person: PlanPerson;
  day: number;
  items: EquipmentItem[];
  mismatch: boolean;
  idle: boolean;
  bind: BindCard;
}) {
  const t = useT();
  const theme = useTheme();
  const label = t('planning.eq.personLabel', { list: items.map((i) => i.label).join(', ') });
  const { image, width } = clusterImage(groupCounts(items), mismatch || idle, theme.palette.text.secondary);

  return (
    <span
      tabIndex={0}
      role="img"
      aria-label={label}
      {...bind(() => <PersonEquipmentCard plan={plan} person={person} day={day} items={items} idle={idle} />)}
      onClick={(event) => event.stopPropagation()}
      style={{
        display: 'inline-block',
        flex: 'none',
        width,
        height: 26,
        borderRadius: 8,
        backgroundColor: theme.palette.action.hover,
        backgroundImage: image,
        backgroundRepeat: 'no-repeat',
      }}
    />
  );
});

function ItemRow({ item, note, warn }: { item: EquipmentItem; note?: string; warn?: boolean }) {
  const t = useT();
  const status = item.notUsable ? t(`planning.eq.status.${item.status}` as MessageKey) : null;

  return (
    <Stack direction="row" spacing={1.25} sx={{ alignItems: 'flex-start' }}>
      <Box component="span" sx={{ display: 'inline-flex', mt: 0.125, color: 'text.secondary', flex: 'none' }}>
        <EquipmentIcon group={item.group} size={22} />
      </Box>
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
          {item.label}
        </Typography>
        <Typography variant="caption" sx={{ display: 'block', color: warn ? warnText : 'text.secondary' }}>
          {[t(groupKey(item.group)), status, note].filter(Boolean).join(' · ')}
        </Typography>
      </Box>
    </Stack>
  );
}

/** What one person holds, on the day the board shows. */
export function PersonEquipmentCard({
  plan,
  person,
  day,
  items,
  idle,
}: {
  plan: Plan;
  person: PlanPerson;
  day: number;
  items: EquipmentItem[];
  idle: boolean;
}) {
  const t = useT();
  const cell = person.cells[day];

  return (
    <Stack spacing={1.25}>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
          {person.name}
        </Typography>
        {plan.date(day) !== plan.today && (
          <Typography variant="caption" color="text.secondary">
            {t('planning.eq.todayState')}
          </Typography>
        )}
      </Box>
      {idle && (
        <Typography variant="caption" sx={{ color: warnText, fontWeight: 600 }}>
          {t(cell.away ? 'planning.eq.idleAway' : 'planning.eq.idleFree')}
        </Typography>
      )}
      {items.map((item) =>
        isMismatch(item, cell.project) ? (
          <ItemRow
            key={item.id}
            item={item}
            note={t('planning.eq.mismatch', { site: plan.projectById.get(item.projectId!)?.name ?? '' })}
            warn
          />
        ) : (
          <ItemRow key={item.id} item={item} />
        ),
      )}
    </Stack>
  );
}

/** The counts under a site's name, one icon per kind, with an amber pill for what sits on the site held by nobody. */
export function SiteEquipmentChips({
  data,
  label,
  hover,
}: {
  data: SiteEquipment;
  label: string;
  hover: HoverBind;
}) {
  const t = useT();
  const theme = useTheme();

  return (
    <span
      tabIndex={0}
      role="img"
      aria-label={label}
      {...hover}
      style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 6, padding: '0 12px 10px' }}
    >
      {groupCounts(data.all).map((g) => (
        <span
          key={g.group}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
            height: 26,
            padding: '0 8px',
            borderRadius: 999,
            background: theme.palette.action.hover,
            color: theme.palette.text.secondary,
            fontSize: 12.5,
            fontWeight: 600,
          }}
        >
          <EquipmentIcon group={g.group} size={18} />
          {g.count}
        </span>
      ))}
      {data.free.length > 0 && (
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            height: 26,
            padding: '0 10px',
            borderRadius: 999,
            background: alpha(EQ_WARN, 0.16),
            color: warnText(theme),
            fontSize: 12.5,
            fontWeight: 700,
          }}
        >
          {t('planning.eq.freeCount', { count: data.free.length })}
        </span>
      )}
    </span>
  );
}

/** A site and all that is on it: each person with what they hold, and what is on the site with nobody holding it. */
export function SiteEquipmentCard({ plan, siteName, day, data }: { plan: Plan; siteName: string; day: number; data: SiteEquipment }) {
  const t = useT();

  return (
    <Stack spacing={1.25}>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
          {siteName}
        </Typography>
        {plan.date(day) !== plan.today && (
          <Typography variant="caption" color="text.secondary">
            {t('planning.eq.todayState')}
          </Typography>
        )}
      </Box>
      {data.held.map(({ person, items }) => (
        <Stack key={person.id} direction="row" spacing={1} sx={{ alignItems: 'center', p: 0.75, borderRadius: 1.5, bgcolor: 'action.hover' }}>
          <Typography variant="body2" sx={{ flex: 1, minWidth: 0, fontWeight: 600, overflowWrap: 'anywhere' }}>
            {person.name}
          </Typography>
          <Stack direction="row" spacing={0.5} sx={{ color: 'text.secondary' }}>
            {groupCounts(items).map((g) => (
              <EquipmentIcon key={g.group} group={g.group} size={20} />
            ))}
          </Stack>
        </Stack>
      ))}
      {data.held.length === 0 && data.free.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          {t('planning.eq.noEquipment')}
        </Typography>
      )}
      {data.free.length > 0 && (
        <Stack spacing={0.75} sx={{ borderTop: 1, borderColor: 'divider', pt: 1.25 }}>
          <Typography variant="caption" sx={{ color: warnText, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '.04em' }}>
            {t('planning.eq.freeHeading')}
          </Typography>
          {data.free.map((item) => (
            <ItemRow key={item.id} item={item} note={t('planning.eq.onSiteNote')} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}

/** Whether a person or a site stands out for the alert the planner clicked. */
export function matchesAlert(alert: EquipmentAlert | null, hit: { free?: boolean; mismatch?: boolean; idle?: boolean }): boolean {
  if (!alert) return true;

  return !!hit[alert];
}
