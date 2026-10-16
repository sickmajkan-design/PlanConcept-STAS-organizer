import { CloseOutlined, DescriptionOutlined } from '@mui/icons-material';
import { Alert, Box, IconButton, MenuItem, Select, Stack, Typography } from '@mui/material';
import { useCallback, useMemo, useState, type ReactNode } from 'react';

import type { AttachmentCategory, AttachmentOwnerType } from '../../api/types';
import { useAttachmentLimits } from '../../features/attachments/useAttachments';
import {
  rememberUnsent,
  uploadAfterCreate,
  type PendingFile,
} from '../../features/files/uploadAfterCreate';
import type { Rejection } from '../../features/files/validateFiles';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { FileDropzone } from './FileDropzone';
import { rejectionText } from './rejectionText';

let counter = 0;

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} kB`;

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Documents picked while a record is still being made. They are only held here; once the form has saved the
 * record and has its id, `send` files them against it, so nobody has to save first and come back to the
 * Documents card. A file that fails never undoes the record: the person lands on it and is told which ones
 * to add there.
 */
export function useDocumentsAtCreate(
  ownerType: AttachmentOwnerType,
  categories: readonly AttachmentCategory[],
) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const limits = useAttachmentLimits();
  const [items, setItems] = useState<PendingFile[]>([]);
  const [rejected, setRejected] = useState<Rejection[]>([]);

  const defaultCategory = categories[0] ?? 'Other';

  const add = useCallback(
    (files: File[]) => {
      setRejected([]);
      setItems((current) => [
        ...current,
        ...files.map((file) => ({ id: `p${(counter += 1)}`, file, category: defaultCategory })),
      ]);
    },
    [defaultCategory],
  );

  const send = useCallback(
    async (ownerId: string): Promise<string[]> => {
      if (items.length === 0) return [];

      const failed = await uploadAfterCreate(ownerType, ownerId, items);
      rememberUnsent(ownerId, failed);

      return failed;
    },
    [items, ownerType],
  );

  const section: ReactNode = useMemo(
    () => (
      <Stack spacing={1.5} data-testid="documents-at-create">
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('files.atCreate.title')}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t('files.atCreate.help')}
          </Typography>
        </Box>

        <FileDropzone
          compact
          accept={limits.accept}
          maxBytes={limits.maxBytes}
          existing={items.map((item) => item.file)}
          onFiles={add}
          onReject={setRejected}
        />

        {rejected.length > 0 && (
          <Alert severity="warning" onClose={() => setRejected([])}>
            {rejected.slice(0, 3).map((r) => (
              <Typography key={`${r.file.name}-${r.reason}`} variant="body2">
                {rejectionText(t, r, limits.maxBytes)}
              </Typography>
            ))}
          </Alert>
        )}

        {items.length > 0 && (
          <Stack component="ul" spacing={0.5} sx={{ m: 0, p: 0, listStyle: 'none' }} aria-label={t('files.queue')}>
            {items.map((item) => (
              <Stack
                key={item.id}
                component="li"
                direction="row"
                spacing={1}
                sx={{ alignItems: 'center', py: 0.5, borderBottom: 1, borderColor: 'divider' }}
              >
                <DescriptionOutlined fontSize="small" color="action" />
                <Typography variant="body2" noWrap sx={{ flex: 1, minWidth: 0 }}>
                  {item.file.name}
                </Typography>
                <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>
                  {formatSize(item.file.size)}
                </Typography>
                {categories.length > 1 && (
                  <Select
                    size="small"
                    variant="standard"
                    disableUnderline
                    value={item.category}
                    onChange={(event) =>
                      setItems((current) =>
                        current.map((c) => (c.id === item.id ? { ...c, category: event.target.value as AttachmentCategory } : c)),
                      )
                    }
                    SelectDisplayProps={{ 'aria-label': `${t('attachments.category')}: ${item.file.name}` }}
                    sx={{ fontSize: 13, maxWidth: 140, flexShrink: 0 }}
                  >
                    {categories.map((value) => (
                      <MenuItem key={value} value={value}>
                        {enumLabel('attachmentCategory', value)}
                      </MenuItem>
                    ))}
                  </Select>
                )}
                <IconButton
                  size="small"
                  aria-label={`${t('files.remove')}: ${item.file.name}`}
                  onClick={() => setItems((current) => current.filter((c) => c.id !== item.id))}
                >
                  <CloseOutlined fontSize="small" />
                </IconButton>
              </Stack>
            ))}
          </Stack>
        )}
      </Stack>
    ),
    [t, enumLabel, limits.accept, limits.maxBytes, items, rejected, add, categories],
  );

  return { section, send, count: items.length };
}
