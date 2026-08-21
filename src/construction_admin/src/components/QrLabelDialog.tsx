import { Button, Dialog, DialogActions, DialogContent, Stack, Typography } from '@mui/material';
import { QRCodeSVG } from 'qrcode.react';

import { useT } from '../i18n/useI18n';

/**
 * A printable label: QR code + name + the raw code underneath, in case the
 * label ever needs to be typed in by hand. Printing renders only this
 * dialog's content, isolated from the rest of the page via `@media print`.
 */
export function QrLabelDialog({
  open,
  onClose,
  title,
  qrCode,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  qrCode: string | null;
}) {
  const t = useT();

  if (!qrCode) {
    return null;
  }

  return (
    <Dialog open={open} onClose={onClose}>
      <DialogContent>
        <Stack
          id="qr-print-label"
          spacing={1.5}
          sx={{ alignItems: 'center', py: 2, px: 1, minWidth: 240 }}
        >
          <QRCodeSVG value={qrCode} size={200} />
          <Typography variant="subtitle1" sx={{ fontWeight: 700, textAlign: 'center' }}>
            {title}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
            {qrCode}
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ '@media print': { display: 'none' } }}>
        <Button onClick={onClose}>{t('common.close')}</Button>
        <Button variant="contained" onClick={() => window.print()}>
          {t('common.print')}
        </Button>
      </DialogActions>

      <style>{`
        @media print {
          body * {
            visibility: hidden;
          }
          #qr-print-label, #qr-print-label * {
            visibility: visible;
          }
          #qr-print-label {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
          }
        }
      `}</style>
    </Dialog>
  );
}
