import {
  ApartmentOutlined,
  CalendarMonthOutlined,
  ChecklistOutlined,
  CampaignOutlined,
  DescriptionOutlined,
  EventBusyOutlined,
  HomeWorkOutlined,
  NotificationsNoneOutlined,
  PlayArrowRounded,
  RequestQuoteOutlined,
  ScheduleOutlined,
  ScheduleSendOutlined,
  ShoppingCartOutlined,
  StopRounded,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';

import { canReviewRefunds, canViewDirectory } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useClockIn, useClockInSites, useClockOut, useCurrentShift } from '../../features/timeEntries/useShift';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatTimeOfDay, splitMinutes } from '../../utils/formatting';

/** One fix when the browser has it. A worker in a basement must still be able to start work, so nothing here blocks. */
export function getPosition(): Promise<{ latitude: number; longitude: number } | null> {
  if (!('geolocation' in navigator)) return Promise.resolve(null);

  return new Promise((resolve) =>
    navigator.geolocation.getCurrentPosition(
      (p) => resolve({ latitude: p.coords.latitude, longitude: p.coords.longitude }),
      () => resolve(null),
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 },
    ),
  );
}

function ShiftCard() {
  const t = useT();
  const current = useCurrentShift();
  const sites = useClockInSites();
  const clockIn = useClockIn();
  const clockOut = useClockOut();
  const [projectId, setProjectId] = useState('');
  const [ending, setEnding] = useState(false);
  const [breakMinutes, setBreakMinutes] = useState('0');
  const [now, setNow] = useState(() => Date.now());
  const [problem, setProblem] = useState<string | null>(null);
  const shift = current.data ?? null;

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 30000);
    return () => window.clearInterval(id);
  }, []);

  const start = async () => {
    setProblem(null);
    const position = await getPosition();

    try {
      await clockIn.mutateAsync({ projectId: projectId || null, ...position });
    } catch (error) {
      setProblem((error as Error).message);
    }
  };

  const end = async () => {
    setProblem(null);
    const position = await getPosition();

    try {
      await clockOut.mutateAsync({ breakMinutes: Math.max(0, Math.round(Number(breakMinutes) || 0)), ...position });
      setEnding(false);
    } catch (error) {
      setEnding(false);
      setProblem((error as Error).message);
    }
  };

  const busy = clockIn.isPending || clockOut.isPending;
  const elapsed = shift ? Math.max(0, Math.floor((now - new Date(shift.startedAt).getTime()) / 60000)) : 0;

  return (
    <Card variant="outlined" sx={{ borderRadius: 3 }}>
      <CardContent>
        <Stack spacing={2}>
          {current.isError && <Alert severity="error">{t('myWork.shift.loadError')}</Alert>}
          {problem && (
            <Alert severity="error" onClose={() => setProblem(null)}>
              {problem}
            </Alert>
          )}

          {shift ? (
            <>
              <Box>
                <Typography variant="overline" color="text.secondary">
                  {t('myWork.shift.running')}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                  {t('timeEntries.hoursShort', splitMinutes(elapsed))}
                </Typography>
                <Typography color="text.secondary">
                  {t('myWork.shift.since', { time: formatTimeOfDay(shift.startedAt) })}
                  {shift.projectName ? ` · ${shift.projectName}` : ''}
                </Typography>
              </Box>
              <Button
                size="large"
                variant="contained"
                color="error"
                startIcon={<StopRounded />}
                disabled={busy}
                onClick={() => setEnding(true)}
                sx={{ py: 1.75, fontSize: '1.1rem' }}
              >
                {t('myWork.shift.end')}
              </Button>
            </>
          ) : (
            <>
              <Box>
                <Typography variant="overline" color="text.secondary">
                  {t('myWork.shift.notRunning')}
                </Typography>
                <Typography color="text.secondary">{t('myWork.shift.locationNote')}</Typography>
              </Box>
              {(sites.data?.length ?? 0) > 1 && (
                <TextField select size="small" label={t('myWork.shift.site')} value={projectId} onChange={(e) => setProjectId(e.target.value)}>
                  <MenuItem value="">{t('myWork.shift.siteAuto')}</MenuItem>
                  {sites.data!.map((s) => (
                    <MenuItem key={s.id} value={s.id}>
                      {s.name}
                    </MenuItem>
                  ))}
                </TextField>
              )}
              <Button
                size="large"
                variant="contained"
                startIcon={<PlayArrowRounded />}
                disabled={busy || current.isLoading}
                onClick={start}
                sx={{ py: 1.75, fontSize: '1.1rem' }}
              >
                {t('myWork.shift.start')}
              </Button>
            </>
          )}
        </Stack>
      </CardContent>

      <Dialog open={ending} onClose={() => !busy && setEnding(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t('myWork.shift.endTitle')}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            type="number"
            label={t('myWork.shift.break')}
            value={breakMinutes}
            onChange={(e) => setBreakMinutes(e.target.value)}
            slotProps={{ htmlInput: { min: 0, max: 600, inputMode: 'numeric' } }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEnding(false)} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button variant="contained" color="error" onClick={end} disabled={busy}>
            {t('myWork.shift.end')}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
}

function Tile({ to, icon, label, hint }: { to: string; icon: ReactNode; label: string; hint: string }) {
  return (
    <Card variant="outlined" sx={{ borderRadius: 3 }}>
      <CardActionArea component={Link} to={to} sx={{ height: '100%', p: 1.5 }}>
        <Stack spacing={0.5}>
          <Box sx={{ color: 'primary.main', display: 'flex' }}>{icon}</Box>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {label}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {hint}
          </Typography>
        </Stack>
      </CardActionArea>
    </Card>
  );
}

/**
 * "My work": what a worker or foreman does most days, on one screen that is built for a phone. It stands in for the
 * app on an iPhone until the iOS app is out; the Android app stays the main route.
 */
export function MyWorkPage() {
  const t = useT();
  const { user } = useAuth();
  const lead = canViewDirectory(user);

  return (
    <Stack spacing={2} sx={{ maxWidth: 640 }}>
      <Typography variant="h5" sx={{ fontWeight: 600 }}>
        {t('myWork.title')}
      </Typography>

      {user?.employeeId ? <ShiftCard /> : <Alert severity="info">{t('myWork.noEmployee')}</Alert>}

      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' }}>
        {/* Own time and leave work for everyone. The rest of the panel is for a foreman and above: the API serves it to nobody else. */}
        <Tile to={paths.myTime} icon={<ScheduleOutlined />} label={t('myWork.tile.time')} hint={t('myWork.tile.timeHint')} />
        <Tile to={paths.myAbsences} icon={<EventBusyOutlined />} label={t('myWork.tile.absences')} hint={t('myWork.tile.absencesHint')} />
        <Tile to={paths.myRefunds} icon={<RequestQuoteOutlined />} label={t('myWork.tile.refunds')} hint={t('myWork.tile.myRefundsHint')} />
        <Tile to={paths.myHousing} icon={<HomeWorkOutlined />} label={t('myWork.tile.housing')} hint={t('myWork.tile.housingHint')} />
        <Tile to={paths.myTasks} icon={<ChecklistOutlined />} label={t('myWork.tile.tasks')} hint={t('myWork.tile.tasksHint')} />
        <Tile to={paths.myDocuments} icon={<DescriptionOutlined />} label={t('myWork.tile.documents')} hint={t('myWork.tile.documentsHint')} />
        {/* Inbox and bulletin are open to every account, a worker included. */}
        <Tile to={paths.notifications} icon={<NotificationsNoneOutlined />} label={t('notifications.title')} hint={t('myWork.tile.notificationsHint')} />
        <Tile to={paths.bulletin} icon={<CampaignOutlined />} label={t('nav.bulletin')} hint={t('myWork.tile.bulletinHint')} />
        {lead && <Tile to={paths.timeEntries} icon={<ScheduleOutlined />} label={t('myWork.tile.team')} hint={t('myWork.tile.teamHint')} />}
        {lead && <Tile to={paths.schedule} icon={<CalendarMonthOutlined />} label={t('nav.schedule')} hint={t('myWork.tile.scheduleHint')} />}
        {lead && <Tile to={paths.workItems} icon={<ChecklistOutlined />} label={t('nav.workItems')} hint={t('myWork.tile.workItemsHint')} />}
        {lead && <Tile to={paths.articleOrders} icon={<ShoppingCartOutlined />} label={t('nav.articleOrders')} hint={t('myWork.tile.ordersHint')} />}
        {canReviewRefunds(user) && <Tile to={paths.refunds} icon={<RequestQuoteOutlined />} label={t('nav.refunds')} hint={t('myWork.tile.refundsHint')} />}
        {lead && <Tile to={paths.weeklyReports} icon={<ScheduleSendOutlined />} label={t('nav.weeklyReports')} hint={t('myWork.tile.weeklyHint')} />}
        {lead && <Tile to={paths.projects} icon={<ApartmentOutlined />} label={t('nav.projects')} hint={t('myWork.tile.projectsHint')} />}
      </Box>

      <Typography variant="caption" color="text.secondary">
        {t('myWork.homeScreenHint')}
      </Typography>
    </Stack>
  );
}
