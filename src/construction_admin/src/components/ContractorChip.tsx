import { Chip, type ChipProps } from '@mui/material';

import { useT } from '../i18n/useI18n';
import { contractorColor } from '../theme';

/** The one way a subcontractor is marked anywhere in the panel: purple, always with the word as well. */
export function ContractorChip(props: Omit<ChipProps, 'label' | 'color' | 'variant'>) {
  const t = useT();

  return (
    <Chip
      size="small"
      {...props}
      label={t('employees.contractorBadge')}
      sx={{
        fontWeight: 600,
        color: contractorColor.text,
        backgroundColor: contractorColor.soft,
        border: `1px solid ${contractorColor.main}`,
        ...props.sx,
      }}
    />
  );
}
