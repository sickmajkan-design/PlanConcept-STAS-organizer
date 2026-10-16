import {
  CheckCircleOutlined,
  CloseOutlined,
  DescriptionOutlined,
  ErrorOutlined,
  ImageOutlined,
  ReplayOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
  IconButton,
  LinearProgress,
  List,
  ListItem,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';

import type { QueueItem } from '../../features/files/useUploadQueue';
import { useT } from '../../i18n/useI18n';

/** The server's refusals are in English; the ones a person can meet here are put in their language. */
function friendlyError(t: ReturnType<typeof useT>, message: string): string {
  if (/content of the file does not match/i.test(message)) return t('files.error.content');
  if (/larger than the/i.test(message)) return t('files.error.size');
  if (/not accepted/i.test(message)) return t('files.error.type');

  return message;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} kB`;

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * The files of an upload, one row each: a picture or an icon, the name cut in the middle so the
 * extension stays visible, the size, and where it stands. Failed rows say why and can be sent again.
 */
export function FileQueueList({
  items,
  onRemove,
  onRetry,
  locked = false,
  categories,
}: {
  items: readonly QueueItem[];
  onRemove: (id: string) => void;
  onRetry: (id: string) => void;
  /** Rows cannot be removed while the upload runs (cancelling is the way out). */
  locked?: boolean;
  /**
   * Lets each file that has not gone out yet have its own type. The caller owns the choice; a file with
   * no choice of its own follows `fallback`.
   */
  categories?: {
    options: readonly { value: string; label: string }[];
    fallback: string;
    chosen: Readonly<Record<string, string>>;
    onChange: (id: string, value: string) => void;
  };
}) {
  const t = useT();

  if (items.length === 0) return null;

  return (
    <List dense disablePadding aria-label={t('files.queue')} sx={{ maxHeight: 240, overflowY: 'auto' }}>
      {items.map((item) => {
        const uploading = item.status === 'uploading';

        return (
          <ListItem
            key={item.id}
            divider
            disableGutters
            sx={{ position: 'relative', gap: 1.5, py: 1, pr: 6, minHeight: 52 }}
            secondaryAction={
              item.status === 'error' || item.status === 'cancelled' ? (
                <Tooltip title={t('files.retry')}>
                  <IconButton size="small" aria-label={t('files.retry')} onClick={() => onRetry(item.id)}>
                    <ReplayOutlined fontSize="small" />
                  </IconButton>
                </Tooltip>
              ) : item.status === 'done' ? (
                <CheckCircleOutlined fontSize="small" color="success" aria-label={t('files.status.done')} />
              ) : (
                <Tooltip title={t('files.remove')}>
                  <span>
                    <IconButton
                      size="small"
                      aria-label={`${t('files.remove')}: ${item.file.name}`}
                      disabled={locked || uploading}
                      onClick={() => onRemove(item.id)}
                    >
                      <CloseOutlined fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              )
            }
          >
            <Box
              sx={{
                width: 32,
                height: 32,
                flexShrink: 0,
                borderRadius: 1,
                bgcolor: 'action.hover',
                display: 'grid',
                placeItems: 'center',
                overflow: 'hidden',
              }}
            >
              {item.previewUrl ? (
                <Box component="img" src={item.previewUrl} alt="" sx={{ width: 32, height: 32, objectFit: 'cover' }} />
              ) : item.file.type.startsWith('image/') ? (
                <ImageOutlined fontSize="small" color="action" />
              ) : (
                <DescriptionOutlined fontSize="small" color="action" />
              )}
            </Box>

            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 600, display: 'flex', minWidth: 0 }}>
                <MiddleEllipsis text={item.file.name} />
              </Typography>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography variant="caption" color="text.secondary">
                  {formatSize(item.file.size)}
                </Typography>
                <Typography
                  variant="caption"
                  color={item.status === 'error' ? 'error' : 'text.secondary'}
                  role={item.status === 'error' ? 'alert' : undefined}
                >
                  {item.status === 'uploading'
                    ? t('files.status.uploading', { percent: Math.round(item.progress * 100) })
                    : item.status === 'error' ? (
                        <>
                          <ErrorOutlined sx={{ fontSize: 14, verticalAlign: 'text-bottom', mr: 0.5 }} />
                          {t('files.status.error')}
                          {item.error ? `: ${friendlyError(t, item.error)}` : ''}
                        </>
                      )
                    : item.status === 'done' ? t('files.status.done')
                    : item.status === 'cancelled' ? t('files.status.cancelled')
                    : t('files.status.queued')}
                </Typography>
              </Stack>
            </Box>

            {categories && categories.options.length > 1 && (item.status === 'queued' || item.status === 'error') && (
              <Select
                size="small"
                variant="standard"
                disableUnderline
                value={categories.chosen[item.id] ?? categories.fallback}
                disabled={locked}
                onChange={(event) => categories.onChange(item.id, event.target.value)}
                SelectDisplayProps={{ 'aria-label': `${t('attachments.category')}: ${item.file.name}` }}
                sx={{ fontSize: 13, maxWidth: 130, flexShrink: 0 }}
              >
                {categories.options.map((o) => (
                  <MenuItem key={o.value} value={o.value}>
                    {o.label}
                  </MenuItem>
                ))}
              </Select>
            )}

            {uploading && (
              <LinearProgress
                variant="determinate"
                value={Math.round(item.progress * 100)}
                aria-label={item.file.name}
                sx={{ position: 'absolute', left: 0, right: 0, bottom: 0, height: 3 }}
              />
            )}
          </ListItem>
        );
      })}
    </List>
  );
}

/** A long name cut in the middle, so the start and the extension both stay in view. */
function MiddleEllipsis({ text }: { text: string }) {
  // A short name fits as it is; only a long one is split so its ending stays visible.
  if (text.length <= 28) {
    return <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', minWidth: 0 }}>{text}</span>;
  }

  const keep = 10;
  const head = text.slice(0, text.length - keep);
  const tail = text.slice(text.length - keep);

  return (
    <>
      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', minWidth: 0 }}>{head}</span>
      <span style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>{tail}</span>
    </>
  );
}

export function RetryAllButton({ onClick, count }: { onClick: () => void; count: number }) {
  const t = useT();

  return (
    <Button variant="contained" startIcon={<ReplayOutlined />} onClick={onClick}>
      {t('files.retryFailed', { count })}
    </Button>
  );
}
