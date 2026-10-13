import { alpha, Box, Typography } from '@mui/material';
import type { ReactNode } from 'react';

import { useFileDrop } from '../../features/files/useFileDrop';
import { useT } from '../../i18n/useI18n';

/**
 * Makes a whole region a place a file can be dropped on, without changing what is inside it. For screens
 * that already have their own "choose a file" control and their own checks: the first dropped file goes to
 * the same handler the control uses, and the region shows that it is a target while a file is over it.
 */
export function DropArea({
  onFile,
  disabled = false,
  children,
}: {
  onFile: (file: File) => void;
  disabled?: boolean;
  children: ReactNode;
}) {
  const t = useT();
  const { isOver, bind } = useFileDrop({
    disabled,
    onFiles: ([first]) => {
      if (first) onFile(first);
    },
  });

  return (
    <Box
      {...bind}
      data-testid="drop-area"
      data-over={isOver ? 'true' : undefined}
      sx={(theme) => ({
        position: 'relative',
        borderRadius: 2,
        outline: isOver ? `2px dashed ${theme.palette.primary.main}` : 'none',
        outlineOffset: 4,
        bgcolor: isOver ? alpha(theme.palette.primary.main, 0.06) : 'transparent',
        transition: theme.transitions.create('background-color', { duration: 120 }),
      })}
    >
      {isOver && (
        <Typography
          variant="subtitle1"
          sx={{
            position: 'absolute',
            inset: 0,
            zIndex: 1,
            display: 'grid',
            placeItems: 'center',
            fontWeight: 700,
            color: 'primary.main',
            pointerEvents: 'none',
          }}
        >
          {t('files.drop.release')}
        </Typography>
      )}
      {children}
    </Box>
  );
}
