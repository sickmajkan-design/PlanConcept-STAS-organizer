import { ContentCopyOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { QRCodeSVG } from 'qrcode.react';
import { useEffect, useRef, useState, type FocusEvent } from 'react';

import { ApiError } from '../api/apiError';
import type { EmployeeInvitation } from '../api/onboarding';
import { useCreateInvitation } from '../features/onboarding/useOnboarding';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { formatDate } from '../utils/formatting';

/** The link a person opens. Built from where the panel is served, so it is right on every installation. */
export function invitationLink(token: string): string {
  return `${window.location.origin}${paths.invite(token)}`;
}

/**
 * Hands an employee a one-time link to create their own account. The link is
 * shown once — the server keeps only a hash — with a copy button and a QR code
 * for a phone.
 */
export function InviteEmployeeDialog({
  employee,
  onClose,
}: {
  employee: { id: string; name: string } | null;
  onClose: () => void;
}) {
  const t = useT();
  const create = useCreateInvitation();
  const [invitation, setInvitation] = useState<EmployeeInvitation | null>(null);
  const [copied, setCopied] = useState(false);

  const resetCreate = create.reset;
  // One link per opening: a re-run of the effect must not mint a second one.
  const requestedFor = useRef<string | null>(null);

  useEffect(() => {
    if (!employee) {
      requestedFor.current = null;
      setInvitation(null);
      setCopied(false);
      resetCreate();
      return;
    }

    // Opening the dialog is the request for a link: one click, not two.
    if (requestedFor.current !== employee.id) {
      requestedFor.current = employee.id;
      setInvitation(null);
      setCopied(false);
      create.mutate(employee.id, { onSuccess: setInvitation });
    }
    // Runs when a different employee is opened, not on every render of `create`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [employee?.id, resetCreate]);

  const link = invitation ? invitationLink(invitation.token) : '';

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
    } catch {
      // Older browsers refuse: select the text so it can be copied by hand.
      document.getElementById('invitation-link')?.focus();
    }
  };

  return (
    <Dialog open={!!employee} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('onboarding.invite.title', { name: employee?.name ?? '' })}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 0.5 }}>
          <Typography variant="body2" color="text.secondary">
            {t('onboarding.invite.intro', { name: employee?.name ?? '' })}
          </Typography>

          {create.isPending && <Typography variant="body2">{t('onboarding.invite.creating')}</Typography>}
          {create.error && (
            <Alert severity="error">
              {/* A bare "Not Found" says nothing to the person who clicked the button. */}
              {create.error instanceof ApiError && create.error.status === 404
                ? t('onboarding.invite.unavailable')
                : create.error.message}
            </Alert>
          )}

          {invitation && (
            <>
              <TextField
                id="invitation-link"
                label={t('onboarding.invite.link')}
                value={link}
                fullWidth
                size="small"
                slotProps={{
                  htmlInput: { readOnly: true, onFocus: (event: FocusEvent<HTMLInputElement>) => event.currentTarget.select() },
                  input: {
                    endAdornment: (
                      <InputAdornment position="end">
                        <Button size="small" startIcon={<ContentCopyOutlined />} onClick={() => void copy()}>
                          {copied ? t('onboarding.invite.copied') : t('onboarding.invite.copy')}
                        </Button>
                      </InputAdornment>
                    ),
                  },
                }}
              />

              <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                <Box sx={{ p: 1.5, bgcolor: '#fff', borderRadius: 1, border: 1, borderColor: 'divider', lineHeight: 0 }}>
                  <QRCodeSVG value={link} size={144} />
                </Box>
                <Stack spacing={0.5} sx={{ flex: 1, minWidth: 200 }}>
                  <Typography variant="body2">{t('onboarding.invite.qrHint')}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t('onboarding.invite.expires', { date: formatDate(invitation.expiresAt) })}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {t('onboarding.invite.once')}
                  </Typography>
                </Stack>
              </Box>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
