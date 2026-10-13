import { CameraAltOutlined, CloudUploadOutlined, InfoOutlined } from '@mui/icons-material';
import { alpha, Box, Button, Stack, Typography, useMediaQuery, useTheme } from '@mui/material';
import { useId, useRef, type ClipboardEvent, type KeyboardEvent } from 'react';

import { ACCEPTED_EXTENSIONS, MAX_ATTACHMENT_BYTES } from '../../api/attachments';
import { useFileDrop } from '../../features/files/useFileDrop';
import { validateFiles, type Rejection } from '../../features/files/validateFiles';
import { useT } from '../../i18n/useI18n';

/**
 * Where files are dropped, clicked for or pasted. It checks them against the rules and hands back what
 * was taken and what was turned away; it knows nothing about uploading.
 *
 * On a phone there is nothing to drag onto, so it shows two plain buttons instead: choose, and take a
 * photo. It is also a button for the keyboard (Enter or Space opens the chooser), so dragging is never the
 * only way in.
 */
export function FileDropzone({
  accept = ACCEPTED_EXTENSIONS,
  maxBytes = MAX_ATTACHMENT_BYTES,
  maxFiles,
  existing,
  multiple = true,
  disabled = false,
  compact = false,
  camera = true,
  onFiles,
  onReject,
  helper,
  helperError = false,
}: {
  accept?: string;
  maxBytes?: number;
  maxFiles?: number;
  /** Files already taken, so repeats and the count are judged against them. */
  existing?: readonly File[];
  multiple?: boolean;
  disabled?: boolean;
  /** One line high, for a single file inside a form. */
  compact?: boolean;
  /** Offers "take a photo" on a phone. */
  camera?: boolean;
  onFiles: (files: File[]) => void;
  onReject?: (rejected: Rejection[]) => void;
  /** Replaces the line under the zone. */
  helper?: string;
  /** Shows that line as a problem. */
  helperError?: boolean;
}) {
  const t = useT();
  const theme = useTheme();
  const phone = useMediaQuery(theme.breakpoints.down('sm'));
  const input = useRef<HTMLInputElement>(null);
  const photo = useRef<HTMLInputElement>(null);
  const helperId = useId();

  const take = (files: File[]) => {
    if (disabled || files.length === 0) return;

    const { accepted, rejected } = validateFiles(multiple ? files : files.slice(0, 1), {
      accept,
      maxBytes,
      maxFiles: multiple ? maxFiles : 1,
      existing,
    });

    // Offered more than one where only one fits: the extra ones are said to be skipped, not silently lost.
    const skipped: Rejection[] = multiple
      ? []
      : files.slice(1).map((file) => ({ file, reason: 'count' as const }));

    if (accepted.length > 0) onFiles(accepted);
    if (rejected.length + skipped.length > 0) onReject?.([...rejected, ...skipped]);
  };

  const { isOver, bind } = useFileDrop({ onFiles: take, disabled });

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      input.current?.click();
    }
  };

  const onPaste = (event: ClipboardEvent) => {
    const files = Array.from(event.clipboardData.files);

    if (files.length > 0) {
      event.preventDefault();
      take(files);
    }
  };

  const limit = Math.round(maxBytes / (1024 * 1024));

  const inputs = (
    <>
      <input
        ref={input}
        hidden
        type="file"
        multiple={multiple}
        accept={accept}
        data-testid="file-input"
        onChange={(event) => {
          take(Array.from(event.target.files ?? []));
          event.target.value = '';
        }}
      />
      <input
        ref={photo}
        hidden
        type="file"
        accept="image/*"
        capture="environment"
        data-testid="photo-input"
        onChange={(event) => {
          take(Array.from(event.target.files ?? []));
          event.target.value = '';
        }}
      />
    </>
  );

  const help = (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'flex-start', mt: 1 }}>
      <InfoOutlined sx={{ fontSize: 16, mt: '2px', color: helperError ? 'error.main' : 'text.secondary' }} />
      <Typography id={helperId} variant="caption" color={helperError ? 'error' : 'text.secondary'}>
        {helper ?? `${t('files.drop.formats')} · ${t('files.drop.max', { size: limit })}`}
      </Typography>
    </Stack>
  );

  if (phone) {
    return (
      <Box {...bind}>
        {inputs}
        <Stack direction="row" spacing={1}>
          <Button
            fullWidth
            variant="outlined"
            startIcon={<CloudUploadOutlined />}
            disabled={disabled}
            onClick={() => input.current?.click()}
          >
            {t('files.choose')}
          </Button>
          {camera && (
            <Button
              fullWidth
              variant="outlined"
              startIcon={<CameraAltOutlined />}
              disabled={disabled}
              onClick={() => photo.current?.click()}
            >
              {t('files.camera')}
            </Button>
          )}
        </Stack>
        {help}
      </Box>
    );
  }

  return (
    <Box>
      {inputs}
      <Box
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-disabled={disabled}
        aria-describedby={helperId}
        aria-label={`${t('files.drop.title')}, ${t('files.drop.or')}`}
        data-testid="dropzone"
        data-over={isOver ? 'true' : undefined}
        onClick={() => !disabled && input.current?.click()}
        onKeyDown={onKeyDown}
        onPaste={onPaste}
        {...bind}
        sx={{
          display: 'flex',
          flexDirection: compact ? 'row' : 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: compact ? 1.5 : 1,
          minHeight: compact ? 72 : 160,
          px: 3,
          py: compact ? 1.5 : 3,
          textAlign: compact ? 'left' : 'center',
          cursor: disabled ? 'not-allowed' : 'pointer',
          opacity: disabled ? 0.6 : 1,
          borderRadius: 3,
          border: '2px dashed',
          borderColor: isOver ? 'primary.main' : 'divider',
          bgcolor: isOver ? alpha(theme.palette.primary.main, 0.08) : 'transparent',
          transition: theme.transitions.create(['border-color', 'background-color'], { duration: 120 }),
          '&:hover': disabled ? undefined : { borderColor: 'primary.main' },
          '&:focus-visible': { outline: `2px solid ${theme.palette.primary.main}`, outlineOffset: 2 },
          '& .dropzone-icon': {
            transition: theme.transitions.create('transform', { duration: 120 }),
            transform: isOver ? 'translateY(-4px)' : 'none',
          },
          '@media (prefers-reduced-motion: reduce)': {
            transition: 'none',
            '& .dropzone-icon': { transition: 'none', transform: 'none' },
          },
        }}
      >
        <CloudUploadOutlined
          className="dropzone-icon"
          sx={{ fontSize: compact ? 28 : 44, color: 'primary.main' }}
        />
        <Box>
          <Typography variant={compact ? 'body2' : 'subtitle1'} sx={{ fontWeight: 700 }}>
            {isOver ? t('files.drop.release') : t('files.drop.title')}
          </Typography>
          {!isOver && (
            <Typography variant="body2" color="text.secondary">
              {t('files.drop.or')}
            </Typography>
          )}
        </Box>
      </Box>
      {help}
    </Box>
  );
}
