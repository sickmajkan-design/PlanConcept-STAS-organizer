import { QrCodeScannerOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import jsQR from 'jsqr';
import { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../api/apiError';
import { toolsApi } from '../api/tools';
import { vehiclesApi } from '../api/vehicles';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

/**
 * Looks a tool or vehicle up from a photo of its printed QR label — the
 * desktop equivalent of the mobile app's camera scan, for a machine with no
 * camera of its own. Decoding happens entirely in the browser; only the
 * decoded code, never the photo, ever reaches the API.
 */
export function FindByQrPhotoDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFile = async (file: File) => {
    setBusy(true);
    setError(null);

    try {
      const code = await decodeQrFromFile(file);

      if (!code) {
        setError(t('qrPhoto.notFound'));
        return;
      }

      try {
        const tool = await toolsApi.getByQrCode(code);
        onClose();
        navigate(paths.toolDetail(tool.id));
        return;
      } catch (toolError) {
        if (toApiError(toolError).status !== 404) {
          throw toolError;
        }
      }

      const vehicle = await vehiclesApi.getByQrCode(code);
      onClose();
      navigate(paths.vehicleDetail(vehicle.id));
    } catch (caught) {
      const apiError = toApiError(caught);
      setError(apiError.status === 404 ? t('qrPhoto.noMatch') : apiError.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('qrPhoto.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography color="text.secondary">{t('qrPhoto.hint')}</Typography>

          {error && <Alert severity="error">{error}</Alert>}

          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            hidden
            onChange={(event) => {
              const file = event.target.files?.[0];
              event.target.value = '';
              if (file) void handleFile(file);
            }}
          />
          <Button
            variant="outlined"
            startIcon={<QrCodeScannerOutlined />}
            disabled={busy}
            loading={busy}
            onClick={() => fileInputRef.current?.click()}
          >
            {t('qrPhoto.choosePhoto')}
          </Button>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          {t('common.cancel')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Draws the image onto an offscreen canvas and reads whatever QR code jsQR finds. */
function decodeQrFromFile(file: File): Promise<string | null> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const image = new Image();

    image.onload = () => {
      try {
        const canvas = document.createElement('canvas');
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;

        const context = canvas.getContext('2d');
        if (!context) {
          resolve(null);
          return;
        }

        context.drawImage(image, 0, 0);
        const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
        const result = jsQR(imageData.data, imageData.width, imageData.height);

        resolve(result?.data ?? null);
      } catch (caught) {
        reject(caught);
      } finally {
        URL.revokeObjectURL(url);
      }
    };

    image.onerror = () => {
      URL.revokeObjectURL(url);
      resolve(null);
    };

    image.src = url;
  });
}
