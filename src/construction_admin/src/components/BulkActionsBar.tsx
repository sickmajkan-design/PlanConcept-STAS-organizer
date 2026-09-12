import { DeleteOutlined } from '@mui/icons-material';
import { Button, Paper, Typography } from '@mui/material';

import { useT } from '../i18n/useI18n';

/**
 * Appears above the grid only once something is checked — a JIRA/Gmail-style
 * "N selected" bar rather than a permanently-visible toolbar, so a list with
 * no bulk work in progress looks exactly like it did before this existed.
 */
export function BulkActionsBar({
  count,
  onDelete,
  onClear,
}: {
  count: number;
  onDelete: () => void;
  onClear: () => void;
}) {
  const t = useT();

  if (count === 0) return null;

  return (
    <Paper
      variant="outlined"
      sx={{
        px: 2,
        py: 1,
        mb: 2,
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        bgcolor: 'action.selected',
      }}
    >
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {t('bulk.selectedCount', { count })}
      </Typography>
      <Button
        size="small"
        color="error"
        startIcon={<DeleteOutlined fontSize="small" />}
        onClick={onDelete}
      >
        {t('bulk.deleteSelected')}
      </Button>
      <Button size="small" color="inherit" onClick={onClear}>
        {t('bulk.clearSelection')}
      </Button>
    </Paper>
  );
}
