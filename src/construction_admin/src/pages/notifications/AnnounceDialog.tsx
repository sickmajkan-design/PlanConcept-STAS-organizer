import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  TextField,
} from '@mui/material';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';

import { toApiError } from '../../api/apiError';
import { roles } from '../../api/types';
import { useSendAnnouncement } from '../../features/notifications/useNotifications';
import {
  announcementFormSchema,
  type AnnouncementFormValues,
} from '../../features/notifications/validation';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';

const emptyValues: AnnouncementFormValues = {
  title: '',
  body: '',
  role: '',
  projectId: '',
};

/**
 * Writes one message to everyone, or to a role, or to one site's crew.
 *
 * Both filters may be set at once and they narrow together — "the foremen on
 * the Danube job" is the useful case, and it is why this is two pickers rather
 * than one audience list.
 */
export function AnnounceDialog({
  open,
  onClose,
  onSent,
}: {
  open: boolean;
  onClose: () => void;
  onSent: (recipients: number) => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: allProjects } = useAllProjectsQuery();
  const send = useSendAnnouncement();

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyValues,
  });

  // Reopening with the previous message still in the box is how the same
  // announcement gets sent twice.
  const clearSendError = send.reset;

  useEffect(() => {
    if (open) {
      reset(emptyValues);
      clearSendError();
    }
  }, [clearSendError, open, reset]);

  const submit = handleSubmit((values) => {
    send.mutate(
      {
        title: values.title,
        body: values.body,
        role: values.role === '' ? null : values.role,
        projectId: values.projectId === '' ? null : values.projectId,
      },
      {
        onSuccess: (recipients) => {
          onSent(recipients);
          onClose();
        },
      },
    );
  });

  const error = send.isError ? toApiError(send.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('notifications.announceTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <Controller
              name="title"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  autoFocus
                  label={t('notifications.subject')}
                  error={!!errors.title}
                  helperText={errors.title?.message}
                />
              )}
            />
          </Grid>

          <Grid size={12}>
            <Controller
              name="body"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  fullWidth
                  multiline
                  minRows={4}
                  label={t('notifications.message')}
                  error={!!errors.body}
                  helperText={errors.body?.message}
                />
              )}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  select
                  fullWidth
                  label={t('notifications.audienceRole')}
                >
                  <MenuItem value="">{t('notifications.everyRole')}</MenuItem>
                  {roles.map((value) => (
                    <MenuItem key={value} value={value}>
                      {enumLabel('role', value)}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <Controller
              name="projectId"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  select
                  fullWidth
                  label={t('notifications.audienceProject')}
                >
                  <MenuItem value="">{t('notifications.everyProject')}</MenuItem>
                  {allProjects?.items.map((project) => (
                    <MenuItem key={project.id} value={project.id}>
                      {project.name}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
          </Grid>

          <Grid size={12}>
            {/* A phone notification cannot be recalled, and the audience is
                the one thing that is easy to get wrong here. */}
            <Alert severity="info">{t('notifications.announceHint')}</Alert>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={send.isPending}
          onClick={() => void submit()}
        >
          {t('notifications.send')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
