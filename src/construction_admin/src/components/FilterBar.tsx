import { FilterListOutlined } from '@mui/icons-material';
import { Box, Button, Collapse, Stack, useMediaQuery, useTheme } from '@mui/material';
import { useState, type ReactNode } from 'react';

import { useT } from '../i18n/useI18n';

/**
 * The search box and the filters above a list.
 *
 * On a desktop it is the row it always was. On a phone the search stays in view and
 * everything else folds behind one "Filters" button, because three stacked
 * dropdowns and two toggles pushed the first row of data below the fold on every
 * list in the panel. The button carries the number of filters in force, so a
 * folded bar never hides the reason the list looks short.
 */
export function FilterBar({
  search,
  children,
  activeCount = 0,
}: {
  /** Always visible. */
  search?: ReactNode;
  /** The dropdowns, toggles and buttons; folded away on a phone. */
  children?: ReactNode;
  /** How many of the folded filters are currently narrowing the list. */
  activeCount?: number;
}) {
  const theme = useTheme();
  const isPhone = useMediaQuery(theme.breakpoints.down('sm'));
  const [open, setOpen] = useState(false);
  const t = useT();

  if (!isPhone) {
    return (
      <Stack
        direction="row"
        spacing={2}
        useFlexGap
        sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}
      >
        {search}
        {children}
      </Stack>
    );
  }

  return (
    <Box sx={{ mb: 2 }}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center' }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>{search}</Box>
        {children && (
          <Button
            variant={activeCount > 0 ? 'contained' : 'outlined'}
            onClick={() => setOpen((value) => !value)}
            aria-expanded={open}
            startIcon={<FilterListOutlined />}
            sx={{ flexShrink: 0 }}
          >
            {activeCount > 0 ? t('common.filtersWithCount', { count: activeCount }) : t('common.filters')}
          </Button>
        )}
      </Stack>
      {children && (
        <Collapse in={open} unmountOnExit>
          <Stack spacing={1.5} useFlexGap sx={{ pt: 1.5 }}>
            {children}
          </Stack>
        </Collapse>
      )}
    </Box>
  );
}
