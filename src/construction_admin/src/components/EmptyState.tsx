import { InboxOutlined, type SvgIconComponent } from '@mui/icons-material';
import { Box, Stack, Typography } from '@mui/material';

export function EmptyState({
  message,
  icon: Icon = InboxOutlined,
}: {
  message: string;
  icon?: SvgIconComponent;
}) {
  return (
    <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
      <Stack spacing={1.5} sx={{ alignItems: 'center' }}>
        <Icon sx={{ fontSize: 40, color: 'text.secondary' }} />
        <Typography color="text.secondary">{message}</Typography>
      </Stack>
    </Box>
  );
}
