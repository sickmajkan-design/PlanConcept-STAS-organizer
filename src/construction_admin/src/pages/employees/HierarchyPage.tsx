import {
  AutoFixHighOutlined,
  BusinessOutlined,
  ManageAccountsOutlined,
  SwapVertOutlined,
} from '@mui/icons-material';
import {
  Avatar,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  organizationRanks,
  type OrganizationHierarchyNode,
  type OrganizationRank,
  type Role,
  type UnlinkedAccount,
} from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useCompanyBrandingQuery } from '../../features/companySettings/useCompanySettings';
import {
  useOrganizationHierarchyQuery,
  useSetEmployeeRank,
} from '../../features/employees/useEmployees';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { suggestRank } from './suggestRank';

/**
 * Where somebody with no rank picked is placed, going by their login role.
 *
 * Only where that is unambiguous. A worker, a foreman and a project manager
 * each map to the one rank that carries their name; an administrator or a
 * super-admin could be anything from the owner to the office clerk, and
 * guessing "Director" for them would put a wrong title on the chart. Those
 * stay unplaced until somebody picks.
 */
function rankFromRole(role: Role | null): OrganizationRank | null {
  switch (role) {
    case null:
    case 'Worker':
      return 'Worker';
    case 'Foreman':
      return 'Foreman';
    case 'ProjectManager':
      return 'ProjectManager';
    default:
      return null;
  }
}

/**
 * One colour per rank, darkest at the top. A step in lightness rather than a
 * different hue for each, so the chart reads as a single descending scale and
 * not as eleven unrelated categories. Capped so white text stays legible.
 */
function accentFor(index: number): string {
  return `hsl(212 55% ${24 + index * 2.6}%)`;
}

const UNPLACED_ACCENT = 'hsl(32 85% 42%)';

function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
}

function PersonCard({
  node,
  accent,
  canEdit,
  onOpen,
  onChangeRank,
}: {
  node: OrganizationHierarchyNode;
  accent: string;
  canEdit: boolean;
  onOpen: () => void;
  onChangeRank: () => void;
}) {
  const t = useT();

  return (
    <Paper
      variant="outlined"
      onClick={onOpen}
      sx={{
        // Wide enough for a full name on two lines: at 196px "Darijo
        // Stanković" and most surnames were cut to "Darijo Stan…", on a chart
        // whose only job is to say who is who.
        width: { xs: '100%', sm: 248 },
        p: 1.25,
        display: 'flex',
        alignItems: 'center',
        gap: 1.25,
        cursor: 'pointer',
        borderLeft: `4px solid ${accent}`,
        transition: 'box-shadow 120ms, transform 120ms',
        '&:hover': { boxShadow: 3, transform: 'translateY(-1px)' },
      }}
    >
      <Avatar sx={{ bgcolor: accent, color: '#fff', width: 38, height: 38, fontSize: '0.85rem' }}>
        {initials(node.fullName)}
      </Avatar>
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography
          variant="body2"
          title={node.fullName}
          sx={{
            fontWeight: 600,
            lineHeight: 1.25,
            overflowWrap: 'anywhere',
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
          }}
        >
          {node.fullName}
        </Typography>
        <Typography
          variant="caption"
          color="text.secondary"
          noWrap
          component="div"
          title={node.position}
        >
          {node.position}
        </Typography>
      </Box>
      {canEdit && (
        <Tooltip title={t('hierarchy.changeRank')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onChangeRank();
            }}
          >
            <SwapVertOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      )}
    </Paper>
  );
}

/** A login with no employee behind it: shown for completeness, with no rank to change. */
function AccountCard({ account, onOpen }: { account: UnlinkedAccount; onOpen: () => void }) {
  const enumLabel = useEnumLabel();

  return (
    <Paper
      variant="outlined"
      onClick={onOpen}
      sx={{
        width: 240,
        p: 1.25,
        display: 'flex',
        alignItems: 'center',
        gap: 1.25,
        cursor: 'pointer',
        borderLeft: '4px solid',
        borderLeftColor: 'text.disabled',
        '&:hover': { boxShadow: 3 },
      }}
    >
      <Avatar sx={{ width: 38, height: 38 }}>
        <ManageAccountsOutlined fontSize="small" />
      </Avatar>
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>
          {account.email}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap component="div">
          {enumLabel('role', account.role)}
        </Typography>
      </Box>
    </Paper>
  );
}

/** The short vertical stroke between one level and the next — what makes it read as a chart. */
function Connector() {
  return (
    <Box
      aria-hidden
      sx={{ width: 2, height: 22, bgcolor: 'divider', mx: 'auto' }}
    />
  );
}

function Tier({
  title,
  hint,
  accent,
  people,
  canEdit,
  onOpen,
  onChangeRank,
}: {
  title: string;
  hint?: string;
  accent: string;
  people: OrganizationHierarchyNode[];
  canEdit: boolean;
  onOpen: (node: OrganizationHierarchyNode) => void;
  onChangeRank: (node: OrganizationHierarchyNode) => void;
}) {
  const t = useT();

  return (
    <Paper
      variant="outlined"
      sx={{ width: '100%', overflow: 'hidden', borderTop: `4px solid ${accent}` }}
    >
      <Stack
        direction="row"
        spacing={1.5}
        sx={{ px: 2, py: 1.25, alignItems: 'center', bgcolor: 'action.hover' }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
        <Chip size="small" label={t('hierarchy.people', { count: people.length })} />
        {hint && (
          <Typography variant="caption" color="text.secondary">
            {hint}
          </Typography>
        )}
      </Stack>
      <Box sx={{ p: 2, display: 'flex', flexWrap: 'wrap', gap: 1.5, justifyContent: 'center' }}>
        {people.map((node) => (
          <PersonCard
            key={node.employeeId}
            node={node}
            accent={accent}
            canEdit={canEdit}
            onOpen={() => onOpen(node)}
            onChangeRank={() => onChangeRank(node)}
          />
        ))}
      </Box>
    </Paper>
  );
}

function RankDialog({
  node,
  onClose,
}: {
  node: OrganizationHierarchyNode | null;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const setRank = useSetEmployeeRank();
  // Seeded from the person being edited each time the dialog opens: the
  // component stays mounted between people, so the value has to be reset
  // where the selection changes rather than left over from the last one.
  const [pick, setPick] = useState<{ forId: string | null; value: OrganizationRank | '' }>({
    forId: null,
    value: '',
  });

  const value = node && pick.forId === node.employeeId ? pick.value : (node?.rank ?? '');

  const close = () => {
    setRank.reset();
    setPick({ forId: null, value: '' });
    onClose();
  };

  return (
    <Dialog open={!!node} onClose={close} fullWidth maxWidth="xs">
      <DialogTitle>{t('hierarchy.rankDialogTitle', { name: node?.fullName ?? '' })}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>{t('hierarchy.rankDialogHint')}</DialogContentText>
        <TextField
          select
          fullWidth
          autoFocus
          label={t('employees.rank')}
          value={value}
          onChange={(event) =>
            node &&
            setPick({
              forId: node.employeeId,
              value: event.target.value as OrganizationRank | '',
            })
          }
          error={!!setRank.error}
          helperText={setRank.error ? toApiError(setRank.error).message : undefined}
        >
          <MenuItem value="">{t('hierarchy.defaultPlacement')}</MenuItem>
          {organizationRanks.map((rank) => (
            <MenuItem key={rank} value={rank}>
              {enumLabel('organizationRank', rank)}
            </MenuItem>
          ))}
        </TextField>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={setRank.isPending}
          onClick={() => {
            if (!node) return;

            setRank.mutate(
              { id: node.employeeId, rank: value || null },
              { onSuccess: close },
            );
          }}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

interface RankSuggestion {
  node: OrganizationHierarchyNode;
  rank: OrganizationRank;
}

/**
 * Ranks guessed from job titles, applied only for the people ticked.
 *
 * A title is free text typed by whoever entered the employee, so this proposes
 * and never decides: nothing is written until the button is pressed, and the
 * person who knows the company can untick anybody who is wrong.
 */
function SuggestRanksDialog({
  suggestions,
  open,
  onClose,
}: {
  suggestions: RankSuggestion[];
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const setRank = useSetEmployeeRank();
  const [unticked, setUnticked] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);
  const [applying, setApplying] = useState(false);

  const chosen = suggestions.filter((s) => !unticked.has(s.node.employeeId));

  const close = () => {
    setUnticked(new Set());
    setError(null);
    onClose();
  };

  const apply = async () => {
    setApplying(true);
    setError(null);

    try {
      // One after another rather than all at once: the person at the keyboard
      // should see a failure stop the run, not a dozen requests race.
      for (const { node, rank } of chosen) {
        await setRank.mutateAsync({ id: node.employeeId, rank });
      }

      close();
    } catch (err) {
      setError(toApiError(err).message);
    } finally {
      setApplying(false);
    }
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('hierarchy.suggestTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 1 }}>{t('hierarchy.suggestHint')}</DialogContentText>
        <Stack>
          {suggestions.map(({ node, rank }) => (
            <FormControlLabel
              key={node.employeeId}
              control={
                <Checkbox
                  checked={!unticked.has(node.employeeId)}
                  onChange={(event) =>
                    setUnticked((prev) => {
                      const next = new Set(prev);
                      if (event.target.checked) next.delete(node.employeeId);
                      else next.add(node.employeeId);
                      return next;
                    })
                  }
                />
              }
              label={
                <Typography variant="body2">
                  <strong>{node.fullName}</strong> · {node.position} →{' '}
                  {enumLabel('organizationRank', rank)}
                </Typography>
              }
            />
          ))}
        </Stack>
        {error && (
          <Typography variant="body2" color="error" sx={{ mt: 1 }}>
            {error}
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={chosen.length === 0 || applying}
          onClick={() => void apply()}
        >
          {t('hierarchy.suggestApply', { count: chosen.length })}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export function HierarchyPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { data, isLoading, isError, error, refetch } = useOrganizationHierarchyQuery();
  const { data: branding } = useCompanyBrandingQuery();
  const [editing, setEditing] = useState<OrganizationHierarchyNode | null>(null);
  const [suggesting, setSuggesting] = useState(false);

  const canEdit = canAdministerAccounts(user);

  const suggestions = useMemo<RankSuggestion[]>(
    () =>
      (data?.people ?? []).flatMap((node) => {
        const rank = node.rank === null ? suggestRank(node.position) : null;
        return rank ? [{ node, rank }] : [];
      }),
    [data],
  );

  const { tiers, unplaced } = useMemo(() => {
    const byRank = new Map<OrganizationRank, OrganizationHierarchyNode[]>();
    const loose: OrganizationHierarchyNode[] = [];

    for (const node of data?.people ?? []) {
      const rank = node.rank ?? rankFromRole(node.role);

      if (!rank) {
        loose.push(node);
        continue;
      }

      byRank.set(rank, [...(byRank.get(rank) ?? []), node]);
    }

    return {
      tiers: organizationRanks
        .map((rank, index) => ({ rank, index, people: byRank.get(rank) ?? [] }))
        .filter((tier) => tier.people.length > 0),
      unplaced: loose,
    };
  }, [data]);

  const companyName = branding?.name || t('nav.appName');
  const accounts = data?.unlinkedAccounts ?? [];
  const nothingToShow =
    !isLoading && !isError && tiers.length === 0 && unplaced.length === 0 && accounts.length === 0;

  return (
    <Box>
      <PageHeader title={t('hierarchy.title')} description={t('hierarchy.description')} />

      {canEdit && suggestions.length > 0 && (
        <Box sx={{ mb: 2 }}>
          <Button
            variant="outlined"
            startIcon={<AutoFixHighOutlined />}
            onClick={() => setSuggesting(true)}
          >
            {t('hierarchy.suggest', { count: suggestions.length })}
          </Button>
        </Box>
      )}

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {nothingToShow && (
        <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
          {t('hierarchy.empty')}
        </Typography>
      )}

      {!isLoading && !isError && !nothingToShow && (
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Paper
            elevation={2}
            sx={{
              px: 3,
              py: 1.5,
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              bgcolor: accentFor(0),
              color: '#fff',
            }}
          >
            <BusinessOutlined />
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              {companyName}
            </Typography>
          </Paper>

          {tiers.map((tier) => (
            <Box
              key={tier.rank}
              sx={{ width: '100%', display: 'flex', flexDirection: 'column' }}
            >
              <Connector />
              <Tier
                title={enumLabel('organizationRank', tier.rank)}
                accent={accentFor(tier.index)}
                people={tier.people}
                canEdit={canEdit}
                onOpen={(node) => navigate(paths.employeeDetail(node.employeeId))}
                onChangeRank={setEditing}
              />
            </Box>
          ))}

          {unplaced.length > 0 && (
            <Box sx={{ width: '100%', display: 'flex', flexDirection: 'column', mt: 3 }}>
              <Tier
                title={t('hierarchy.unplaced')}
                hint={t('hierarchy.unplacedHint')}
                accent={UNPLACED_ACCENT}
                people={unplaced}
                canEdit={canEdit}
                onOpen={(node) => navigate(paths.employeeDetail(node.employeeId))}
                onChangeRank={setEditing}
              />
            </Box>
          )}
          {accounts.length > 0 && (
            <Paper
              variant="outlined"
              sx={{ width: '100%', mt: 3, overflow: 'hidden', borderTop: '4px solid', borderTopColor: 'text.disabled' }}
            >
              <Stack
                direction="row"
                spacing={1.5}
                sx={{ px: 2, py: 1.25, alignItems: 'center', bgcolor: 'action.hover' }}
              >
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {t('hierarchy.accounts')}
                </Typography>
                <Chip size="small" label={t('hierarchy.people', { count: accounts.length })} />
                <Typography variant="caption" color="text.secondary">
                  {t('hierarchy.accountsHint')}
                </Typography>
              </Stack>
              <Box sx={{ p: 2, display: 'flex', flexWrap: 'wrap', gap: 1.5, justifyContent: 'center' }}>
                {accounts.map((account) => (
                  <AccountCard
                    key={account.userId}
                    account={account}
                    onOpen={() => navigate(paths.userEdit(account.userId))}
                  />
                ))}
              </Box>
            </Paper>
          )}
        </Box>
      )}

      <RankDialog node={editing} onClose={() => setEditing(null)} />
      <SuggestRanksDialog
        suggestions={suggestions}
        open={suggesting}
        onClose={() => setSuggesting(false)}
      />
    </Box>
  );
}
