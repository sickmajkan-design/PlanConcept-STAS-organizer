import { AddOutlined, CloseOutlined, SendOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  FormControlLabel,
  Menu,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { roles, type AudienceTarget, type Role } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { useBranchesQuery } from '../../features/branches/useBranches';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllNotificationGroupsQuery } from '../../features/notificationGroups/useNotificationGroups';
import { useAudiencePreviewQuery, useSendBroadcast } from '../../features/notifications/useNotifications';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

/** A choice of the audience with the name it is shown by. */
interface Choice {
  target: AudienceTarget;
  label: string;
}

const sameTarget = (a: AudienceTarget, b: AudienceTarget) =>
  a.kind === b.kind && (a.id ?? null) === (b.id ?? null) && (a.role ?? null) === (b.role ?? null);

type Adding = 'Group' | 'Project' | 'Branch' | 'Role' | null;

/**
 * A new announcement: who it is for is built from several choices (a worker, a group, a site's crew, a business unit,
 * a role, everybody), and the people they name are listed at once, each counted once, so the sender sees exactly who
 * will get it before sending.
 */
export function NewAnnouncementPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const { data: employeesPage } = useAllEmployeesQuery();
  const { data: groupsPage } = useAllNotificationGroupsQuery();
  const { data: projectsPage } = useAllProjectsQuery();
  const { data: branchList } = useBranchesQuery();
  const send = useSendBroadcast();

  const employees = employeesPage?.items ?? [];
  const groups = groupsPage?.items ?? [];
  const projects = projectsPage?.items ?? [];
  const branches = (branchList ?? []).filter((b) => b.isActive);

  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [requiresAcknowledgment, setRequiresAcknowledgment] = useState(false);
  const [choices, setChoices] = useState<Choice[]>([]);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [adding, setAdding] = useState<Adding>(null);
  const [workerSearchOpen, setWorkerSearchOpen] = useState(false);
  const [confirmingEveryone, setConfirmingEveryone] = useState(false);

  // From a worker's card: the message starts addressed to that person.
  const preselected = searchParams.get('employee');
  useEffect(() => {
    if (!preselected || choices.length > 0) return;
    const employee = employees.find((e) => e.id === preselected);
    if (employee) {
      setChoices([{ target: { kind: 'Employee', id: employee.id }, label: `${t('notifications.kind.Employee')}: ${employee.firstName} ${employee.lastName}` }]);
    }
    // Only once the employees have loaded.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preselected, employees.length]);

  const targets = useMemo(() => choices.map((c) => c.target), [choices]);
  const { data: preview, isFetching } = useAudiencePreviewQuery(targets);

  const add = (choice: Choice) => {
    setChoices((current) => (current.some((c) => sameTarget(c.target, choice.target)) ? current : [...current, choice]));
    setAdding(null);
    setMenuAnchor(null);
  };

  const remove = (index: number) => setChoices((current) => current.filter((_c, i) => i !== index));

  const hasEveryone = choices.some((c) => c.target.kind === 'Everyone');
  const reachable = preview?.reachable ?? 0;
  const canSend = title.trim() !== '' && body.trim() !== '' && choices.length > 0 && reachable > 0 && !send.isPending;

  const submit = () =>
    send.mutate(
      { title: title.trim(), body: body.trim(), targets, requiresAcknowledgment },
      { onSuccess: (result) => navigate(paths.notificationBroadcast(result.broadcastId)) },
    );

  const error = send.isError ? toApiError(send.error) : null;

  const chosenWorkerIds = new Set(choices.filter((c) => c.target.kind === 'Employee').map((c) => c.target.id));
  const options: Record<Exclude<Adding, null>, { label: string; target: AudienceTarget }[]> = {
    Group: groups.map((g) => ({ label: `${t('notifications.kind.Group')}: ${g.name}`, target: { kind: 'Group', id: g.id } })),
    Project: projects.map((p) => ({ label: `${t('notifications.kind.Project')}: ${p.name}`, target: { kind: 'Project', id: p.id } })),
    Branch: branches.map((b) => ({ label: `${t('notifications.kind.Branch')}: ${b.name}`, target: { kind: 'Branch', id: b.id } })),
    Role: roles
      .filter((r) => r !== 'Customer')
      .map((r) => ({ label: `${t('notifications.kind.Role')}: ${enumLabel('role', r)}`, target: { kind: 'Role', role: r as Role } })),
  };

  const openMenu = (kind: Exclude<Adding, null>) => (event: React.MouseEvent<HTMLElement>) => {
    setAdding(kind);
    setMenuAnchor(event.currentTarget);
  };

  return (
    <Box>
      <PageHeader
        title={t('notifications.newTitle')}
        description={t('notifications.newDescription')}
        secondaryActions={
          <Button onClick={() => navigate(paths.notificationsSent)}>{t('notifications.sentTitle')}</Button>
        }
      />

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1.25fr) minmax(0, 1fr)' },
          alignItems: 'start',
        }}
      >
        <Card variant="outlined">
          <CardContent>
            <Stack spacing={2.5}>
              <TextField
                label={t('notifications.subject')}
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 256 } }}
                fullWidth
              />
              <TextField
                label={t('notifications.message')}
                value={body}
                onChange={(event) => setBody(event.target.value)}
                slotProps={{ htmlInput: { maxLength: 4000 } }}
                multiline
                minRows={4}
                fullWidth
              />

              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
                  {t('notifications.audience')}
                </Typography>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  {t('notifications.audienceHint')}
                </Typography>
                <Stack
                  direction="row"
                  spacing={1}
                  useFlexGap
                  sx={{ flexWrap: 'wrap', p: 1, border: 1, borderColor: 'divider', borderRadius: 1, minHeight: 52, alignItems: 'center' }}
                >
                  {choices.map((choice, index) => {
                    const count = preview?.chips[index]?.count;
                    return (
                      <Chip
                        key={`${choice.target.kind}-${choice.target.id ?? choice.target.role ?? ''}`}
                        color="warning"
                        variant="outlined"
                        label={count == null ? choice.label : `${choice.label} · ${count}`}
                        onDelete={() => remove(index)}
                        deleteIcon={<CloseOutlined aria-label={t('notifications.removeChoice')} />}
                      />
                    );
                  })}
                  {choices.length === 0 && (
                    <Typography variant="body2" color="text.secondary">
                      {t('notifications.audienceEmpty')}
                    </Typography>
                  )}
                </Stack>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  {t('notifications.addRecipients')}
                </Typography>
                <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                  <Button size="small" variant="outlined" startIcon={<AddOutlined />} onClick={() => setWorkerSearchOpen((v) => !v)}>
                    {t('notifications.kind.Employee')}
                  </Button>
                  <Button size="small" variant="outlined" startIcon={<AddOutlined />} onClick={openMenu('Group')}>
                    {t('notifications.kind.Group')}
                  </Button>
                  <Button size="small" variant="outlined" startIcon={<AddOutlined />} onClick={openMenu('Project')}>
                    {t('notifications.kind.Project')}
                  </Button>
                  <Button size="small" variant="outlined" startIcon={<AddOutlined />} onClick={openMenu('Branch')}>
                    {t('notifications.kind.Branch')}
                  </Button>
                  <Button size="small" variant="outlined" startIcon={<AddOutlined />} onClick={openMenu('Role')}>
                    {t('notifications.kind.Role')}
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<AddOutlined />}
                    disabled={hasEveryone}
                    onClick={() => add({ target: { kind: 'Everyone' }, label: t('notifications.kind.Everyone') })}
                  >
                    {t('notifications.kind.Everyone')}
                  </Button>
                </Stack>

                {workerSearchOpen && (
                  <Autocomplete
                    size="small"
                    sx={{ mt: 1.5, maxWidth: 420 }}
                    options={employees.filter((e) => !chosenWorkerIds.has(e.id))}
                    getOptionLabel={(e) => `${e.firstName} ${e.lastName}`}
                    value={null}
                    blurOnSelect
                    clearOnBlur
                    onChange={(_event, employee) => {
                      if (!employee) return;
                      add({
                        target: { kind: 'Employee', id: employee.id },
                        label: `${t('notifications.kind.Employee')}: ${employee.firstName} ${employee.lastName}`,
                      });
                    }}
                    renderInput={(params) => <TextField {...params} label={t('notifications.searchWorker')} autoFocus />}
                  />
                )}

                <Menu
                  anchorEl={menuAnchor}
                  open={!!adding && !!menuAnchor}
                  onClose={() => {
                    setAdding(null);
                    setMenuAnchor(null);
                  }}
                  slotProps={{ paper: { sx: { maxHeight: 360 } } }}
                >
                  {adding &&
                    options[adding]
                      .filter((option) => !choices.some((c) => sameTarget(c.target, option.target)))
                      .map((option) => (
                        <MenuItem key={option.label} onClick={() => add(option)}>
                          {option.label}
                        </MenuItem>
                      ))}
                  {adding && options[adding].length === 0 && <MenuItem disabled>{t('notifications.noOptions')}</MenuItem>}
                </Menu>
              </Box>

              <Box>
                <FormControlLabel
                  control={<Switch checked={requiresAcknowledgment} onChange={(event) => setRequiresAcknowledgment(event.target.checked)} />}
                  label={t('notifications.requiresAcknowledgment')}
                />
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                  {t('notifications.requiresAcknowledgmentHelp')}
                </Typography>
              </Box>

              {error && <Alert severity="error">{error.message}</Alert>}

              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Typography variant="caption" color="text.secondary">
                  {t('notifications.sendNote')}
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<SendOutlined />}
                  disabled={!canSend}
                  onClick={() => (hasEveryone ? setConfirmingEveryone(true) : submit())}
                >
                  {t('notifications.sendTo', { count: reachable })}
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Card variant="outlined" sx={{ position: { md: 'sticky' }, top: { md: 16 } }}>
          <CardContent>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {t('notifications.recipients')}
              </Typography>
              <Typography variant="h4" color="warning.main" sx={{ fontWeight: 700 }}>
                {reachable}
              </Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {choices.length === 0
                ? t('notifications.recipientsEmpty')
                : t('notifications.recipientsSummary', { total: preview?.total ?? 0, sources: choices.length, reachable })}
              {isFetching && ' …'}
            </Typography>

            {!!preview && preview.withoutAccount > 0 && (
              <Alert severity="warning" sx={{ mb: 1 }}>
                {t('notifications.withoutAccountWarning', { count: preview.withoutAccount })}
              </Alert>
            )}

            <Divider />
            <Box sx={{ maxHeight: 460, overflow: 'auto' }}>
              {(preview?.people ?? []).map((person) => (
                <Stack
                  key={person.employeeId ?? person.userId ?? person.name}
                  direction="row"
                  sx={{ justifyContent: 'space-between', alignItems: 'center', py: 0.75, borderBottom: 1, borderColor: 'divider', gap: 1 }}
                >
                  <Typography variant="body2" sx={{ minWidth: 0 }} noWrap>
                    {person.name}{' '}
                    <Typography component="span" variant="caption" color="text.secondary">
                      · {person.role}
                    </Typography>
                  </Typography>
                  {person.hasAccount ? (
                    <Chip size="small" variant="outlined" label={person.via.length > 1 ? t('notifications.sources', { count: person.via.length }) : person.via[0]} />
                  ) : (
                    <Chip size="small" color="warning" variant="outlined" label={t('notifications.noAccount')} />
                  )}
                </Stack>
              ))}
              {choices.length > 0 && preview && preview.people.length === 0 && (
                <Typography color="text.secondary" sx={{ py: 2 }}>
                  {t('notifications.nobody')}
                </Typography>
              )}
            </Box>
          </CardContent>
        </Card>
      </Box>

      <ConfirmDialog
        open={confirmingEveryone}
        title={t('notifications.everyoneTitle')}
        description={t('notifications.everyoneBody', { count: reachable })}
        confirmLabel={t('notifications.send')}
        onConfirm={() => {
          setConfirmingEveryone(false);
          submit();
        }}
        onCancel={() => setConfirmingEveryone(false)}
      />
    </Box>
  );
}
