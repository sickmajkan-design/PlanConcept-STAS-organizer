import { Box, CircularProgress } from '@mui/material';

export function CostsLoading() {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
      <CircularProgress />
    </Box>
  );
}
