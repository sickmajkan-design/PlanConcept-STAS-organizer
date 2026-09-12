import { CloseOutlined, DragIndicatorOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  IconButton,
  Stack,
  Tooltip,
} from '@mui/material';
import type { ReactNode } from 'react';

import { useT } from '../../../i18n/useI18n';

interface WidgetShellProps {
  title: string;
  isLoading?: boolean;
  error?: unknown;
  onRemove?: () => void;
  dragHandleProps?: Record<string, unknown>;
  children: ReactNode;
}

/** The card frame every dashboard widget shares — title, drag handle, remove affordance, loading/error states. */
export function WidgetShell({
  title,
  isLoading,
  error,
  onRemove,
  dragHandleProps,
  children,
}: WidgetShellProps) {
  const t = useT();

  return (
    <Card variant="outlined">
      <CardHeader
        title={title}
        slotProps={{ title: { variant: 'subtitle1', sx: { fontWeight: 700 } } }}
        action={
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            {dragHandleProps && (
              <Box
                {...dragHandleProps}
                sx={{ display: 'flex', cursor: 'grab', color: 'text.disabled' }}
              >
                <DragIndicatorOutlined fontSize="small" />
              </Box>
            )}
            {onRemove && (
              <Tooltip title={t('dashboard.removeWidget')}>
                <IconButton
                  size="small"
                  onClick={onRemove}
                  aria-label={t('dashboard.removeWidget')}
                >
                  <CloseOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        }
        sx={{ pb: 0 }}
      />
      <CardContent>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={24} />
          </Box>
        ) : error ? (
          <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  );
}
