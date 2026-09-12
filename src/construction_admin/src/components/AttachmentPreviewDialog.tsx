import { DownloadOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { attachmentsApi } from '../api/attachments';
import type { Attachment } from '../api/types';
import { useT } from '../i18n/useI18n';

type PreviewState =
  | { kind: 'loading' }
  | { kind: 'image'; url: string }
  | { kind: 'pdf'; url: string }
  | { kind: 'text'; text: string }
  | { kind: 'html'; html: string; note?: string }
  | { kind: 'unsupported' }
  | { kind: 'error' };

function extensionOf(fileName: string): string {
  const dot = fileName.lastIndexOf('.');

  return dot === -1 ? '' : fileName.slice(dot + 1).toLowerCase();
}

const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'webp'];
const HEIC_EXTENSIONS = ['heic', 'heif'];
const EXCEL_EXTENSIONS = ['xls', 'xlsx'];

/**
 * Strips anything a parsed document could use to run script or reach off the
 * page before it is handed to `dangerouslySetInnerHTML` — mammoth and
 * SheetJS both build real HTML from file content, so a crafted file name,
 * cell value, or paragraph is attacker-controlled text by the time it gets
 * here.
 */
function sanitizeHtml(html: string): string {
  const parsed = new DOMParser().parseFromString(html, 'text/html');

  parsed
    .querySelectorAll('script, style, iframe, object, embed, link, meta')
    .forEach((el) => el.remove());

  parsed.querySelectorAll('*').forEach((el) => {
    for (const attr of [...el.attributes]) {
      const name = attr.name.toLowerCase();
      const value = attr.value.trim().toLowerCase();

      if (name.startsWith('on') || ((name === 'href' || name === 'src') && value.startsWith('javascript:'))) {
        el.removeAttribute(attr.name);
      }
    }
  });

  return parsed.body.innerHTML;
}

/**
 * A large in-place look at any uploaded document, opened on double-click
 * instead of making every look a download.
 *
 * Every accepted file type gets a real preview: images render directly, PDFs
 * in an iframe, HEIC photos are converted client-side first (browsers cannot
 * display HEIC), plain text is shown as text, and Word/Excel files are
 * parsed into HTML — all through libraries loaded on demand so a user who
 * never opens an office document never pays for the bundle weight. Only the
 * legacy binary `.doc` format has no parser available; it still downloads.
 */
export function AttachmentPreviewDialog({
  attachment,
  onClose,
}: {
  attachment: Attachment | null;
  onClose: () => void;
}) {
  const t = useT();
  const [preview, setPreview] = useState<PreviewState>({ kind: 'loading' });
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!attachment) {
      setPreview({ kind: 'loading' });
      setDownloadUrl(null);
      return;
    }

    let cancelled = false;
    let downloadObjectUrl: string | undefined;
    let previewObjectUrl: string | undefined;

    setPreview({ kind: 'loading' });
    setDownloadUrl(null);

    void (async () => {
      let blob: Blob;

      try {
        blob = await attachmentsApi.blob(attachment.id);
      } catch {
        // A fetch failure (the file is recorded but missing from storage, a
        // permission change, a dropped connection) is not the same problem
        // as "we have the bytes but cannot render this format" below — the
        // two used to collapse into one "no preview" message, which sent
        // someone looking for a format problem that was not there.
        if (!cancelled) setPreview({ kind: 'error' });
        return;
      }

      try {
        if (cancelled) return;

        downloadObjectUrl = URL.createObjectURL(blob);
        setDownloadUrl(downloadObjectUrl);

        const ext = extensionOf(attachment.fileName);

        if (HEIC_EXTENSIONS.includes(ext)) {
          const heic2any = (await import('heic2any')).default;
          const converted = await heic2any({ blob, toType: 'image/jpeg', quality: 0.85 });
          const jpegBlob = Array.isArray(converted) ? converted[0] : converted;

          if (cancelled) return;

          previewObjectUrl = URL.createObjectURL(jpegBlob);
          setPreview({ kind: 'image', url: previewObjectUrl });
          return;
        }

        if (attachment.contentType.startsWith('image/') || IMAGE_EXTENSIONS.includes(ext)) {
          setPreview({ kind: 'image', url: downloadObjectUrl });
          return;
        }

        if (ext === 'pdf' || attachment.contentType === 'application/pdf') {
          setPreview({ kind: 'pdf', url: downloadObjectUrl });
          return;
        }

        if (ext === 'txt') {
          setPreview({ kind: 'text', text: await blob.text() });
          return;
        }

        if (ext === 'docx') {
          const mammoth = await import('mammoth');
          const arrayBuffer = await blob.arrayBuffer();
          const result = await mammoth.convertToHtml({ arrayBuffer });

          if (cancelled) return;

          setPreview({ kind: 'html', html: sanitizeHtml(result.value) });
          return;
        }

        if (EXCEL_EXTENSIONS.includes(ext)) {
          const XLSX = await import('xlsx');
          const arrayBuffer = await blob.arrayBuffer();
          const workbook = XLSX.read(arrayBuffer, { type: 'array' });
          const [firstSheetName, ...restSheetNames] = workbook.SheetNames;
          const sheet = firstSheetName ? workbook.Sheets[firstSheetName] : undefined;

          if (cancelled) return;

          if (!sheet) {
            setPreview({ kind: 'unsupported' });
            return;
          }

          setPreview({
            kind: 'html',
            html: sanitizeHtml(XLSX.utils.sheet_to_html(sheet)),
            note:
              restSheetNames.length > 0
                ? t('attachments.moreSheets', { count: restSheetNames.length })
                : undefined,
          });
          return;
        }

        // `.doc` (legacy binary Word) and anything else outside the
        // accepted list: no client-side parser exists for it.
        setPreview({ kind: 'unsupported' });
      } catch {
        if (!cancelled) {
          setPreview({ kind: 'unsupported' });
        }
      }
    })();

    return () => {
      cancelled = true;

      if (downloadObjectUrl) URL.revokeObjectURL(downloadObjectUrl);
      if (previewObjectUrl) URL.revokeObjectURL(previewObjectUrl);
    };
  }, [attachment, t]);

  return (
    <Dialog open={!!attachment} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{attachment?.fileName}</DialogTitle>
      <DialogContent>
        {preview.kind === 'loading' && (
          <Stack sx={{ alignItems: 'center', py: 6 }}>
            <CircularProgress size={28} />
          </Stack>
        )}

        {preview.kind === 'image' && (
          <Box
            component="img"
            src={preview.url}
            alt={attachment?.fileName}
            sx={{ display: 'block', maxWidth: '100%', maxHeight: '75vh', mx: 'auto' }}
          />
        )}

        {preview.kind === 'pdf' && (
          <Box
            component="iframe"
            src={preview.url}
            title={attachment?.fileName}
            sx={{ width: '100%', height: '75vh', border: 0 }}
          />
        )}

        {preview.kind === 'text' && (
          <Box
            component="pre"
            sx={{
              m: 0,
              p: 2,
              maxHeight: '75vh',
              overflow: 'auto',
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              fontFamily: 'monospace',
              fontSize: '0.85rem',
              bgcolor: 'action.hover',
              borderRadius: 1,
            }}
          >
            {preview.text}
          </Box>
        )}

        {preview.kind === 'html' && (
          <Stack spacing={1}>
            <Box
              sx={{
                maxHeight: '75vh',
                overflow: 'auto',
                p: 1,
                '& table': { borderCollapse: 'collapse', width: '100%' },
                '& td, & th': {
                  border: '1px solid',
                  borderColor: 'divider',
                  p: 0.75,
                  fontSize: '0.85rem',
                },
                '& img': { maxWidth: '100%' },
              }}
              dangerouslySetInnerHTML={{ __html: preview.html }}
            />
            {preview.note && (
              <Typography variant="caption" color="text.secondary">
                {preview.note}
              </Typography>
            )}
          </Stack>
        )}

        {preview.kind === 'unsupported' && (
          <Stack spacing={1} sx={{ py: 4, alignItems: 'center' }}>
            <Typography color="text.secondary">{t('attachments.noPreview')}</Typography>
          </Stack>
        )}

        {preview.kind === 'error' && (
          <Stack spacing={1} sx={{ py: 4, alignItems: 'center' }}>
            <Typography color="error">{t('attachments.previewError')}</Typography>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        {downloadUrl && attachment && (
          <Button
            component="a"
            href={downloadUrl}
            download={attachment.fileName}
            startIcon={<DownloadOutlined />}
          >
            {t('attachments.download')}
          </Button>
        )}
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
