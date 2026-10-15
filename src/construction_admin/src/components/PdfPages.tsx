import { Alert, Box, CircularProgress, Stack, Typography } from '@mui/material';
import { useEffect, useRef, useState } from 'react';

import { useT } from '../i18n/useI18n';

/** More pages than this are not drawn; the document can still be downloaded whole. */
const MAX_PAGES = 40;

/**
 * Draws a PDF's pages itself instead of handing the file to the browser's own viewer.
 *
 * Phones very often cannot show a PDF in a frame (iOS Safari shows only the first page, many Android
 * browsers show nothing), and the page's content-security policy has to allow frames and objects for the
 * built-in viewer to work at all. Drawing the pages onto canvases works everywhere and needs neither.
 *
 * pdf.js is loaded on demand, and its legacy build is used so older phones are covered too.
 */
export function PdfPages({ blob, onFailed }: { blob: Blob; onFailed: () => void }) {
  const t = useT();
  const host = useRef<HTMLDivElement | null>(null);
  const [state, setState] = useState<{ status: 'loading' } | { status: 'ready'; pages: number; shown: number }>({
    status: 'loading',
  });

  useEffect(() => {
    let cancelled = false;
    let destroy: (() => void) | undefined;

    void (async () => {
      try {
        const pdfjs = await import('pdfjs-dist/legacy/build/pdf.mjs');

        pdfjs.GlobalWorkerOptions.workerSrc = new URL(
          'pdfjs-dist/legacy/build/pdf.worker.min.mjs',
          import.meta.url,
        ).toString();

        const data = new Uint8Array(await blob.arrayBuffer());
        const loading = pdfjs.getDocument({ data });
        destroy = () => void loading.destroy();

        const document = await loading.promise;

        if (cancelled || !host.current) return;

        const total = document.numPages;
        const shown = Math.min(total, MAX_PAGES);
        const width = Math.max(host.current.clientWidth, 280);
        const ratio = Math.min(window.devicePixelRatio || 1, 2);

        host.current.replaceChildren();

        for (let number = 1; number <= shown; number++) {
          const page = await document.getPage(number);

          if (cancelled || !host.current) return;

          const unscaled = page.getViewport({ scale: 1 });
          const scale = width / unscaled.width;
          const viewport = page.getViewport({ scale: scale * ratio });

          const canvas = window.document.createElement('canvas');
          canvas.width = Math.floor(viewport.width);
          canvas.height = Math.floor(viewport.height);
          canvas.style.width = `${Math.floor(viewport.width / ratio)}px`;
          canvas.style.height = `${Math.floor(viewport.height / ratio)}px`;
          canvas.style.display = 'block';
          canvas.style.margin = '0 auto 12px';
          canvas.style.maxWidth = '100%';
          canvas.setAttribute('role', 'img');
          canvas.setAttribute('aria-label', t('attachments.pdfPage', { number, count: total }));

          host.current.appendChild(canvas);

          await page.render({ canvas, viewport }).promise;
        }

        if (!cancelled) setState({ status: 'ready', pages: total, shown });
      } catch {
        // Anything that stops the pages being drawn — a damaged file, an old browser — falls back to the
        // browser's own viewer, which is what this replaced.
        if (!cancelled) onFailed();
      }
    })();

    return () => {
      cancelled = true;
      destroy?.();
    };
    // The blob identifies the document; a new one starts over.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blob]);

  return (
    <Stack spacing={1}>
      {state.status === 'loading' && (
        <Stack sx={{ alignItems: 'center', py: 3 }}>
          <CircularProgress size={28} />
        </Stack>
      )}
      <Box ref={host} data-testid="pdf-pages" sx={{ maxHeight: '75vh', overflow: 'auto' }} />
      {state.status === 'ready' && state.shown < state.pages && (
        <Alert severity="info">{t('attachments.pdfMorePages', { shown: state.shown, count: state.pages })}</Alert>
      )}
      {state.status === 'ready' && state.pages > 1 && state.shown >= state.pages && (
        <Typography variant="caption" color="text.secondary">
          {t('attachments.pdfPageCount', { count: state.pages })}
        </Typography>
      )}
    </Stack>
  );
}
