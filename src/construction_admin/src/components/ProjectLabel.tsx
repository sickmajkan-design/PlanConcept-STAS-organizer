import { Box, Typography } from '@mui/material';
import { useT } from '../i18n/useI18n';

import { useProjectCustomers } from '../features/projects/useProjectCustomers';

interface ProjectLabelProps {
  name: string;
  projectId?: string | null;
  /** Known already (the screen's own data carries it), so no lookup is needed. */
  customerName?: string | null;
  /** `inline`: the customer sits beside the name. `stacked`: under it, for narrow places. */
  variant?: 'inline' | 'stacked';
  /** Name is rendered in a single line with an ellipsis when the place is tight. */
  noWrap?: boolean;
}

/** The project name with its customer next to it. Use this wherever a project name is shown. */
export function ProjectLabel({ name, projectId, customerName, variant = 'inline', noWrap }: ProjectLabelProps) {
  const t = useT();
  const { data } = useProjectCustomers();

  let customer = customerName;
  let known = customerName !== undefined;
  if (!known && data) {
    const map = projectId && data.byId.has(projectId) ? data.byId : data.byName;
    const key = projectId && data.byId.has(projectId) ? projectId : name;
    if (map.has(key)) {
      customer = map.get(key);
      known = true;
    }
  }

  const stacked = variant === 'stacked';

  return (
    <Box
      component="span"
      sx={{
        display: stacked ? 'inline-flex' : 'inline',
        flexDirection: 'column',
        minWidth: 0,
        maxWidth: '100%',
        verticalAlign: 'top',
      }}
    >
      <Box component="span" sx={noWrap ? { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } : undefined}>
        {name}
      </Box>
      {known && (
        <Typography
          component="span"
          variant="caption"
          noWrap={noWrap}
          sx={{
            ml: stacked ? 0 : 0.75,
            color: customer ? 'secondary.main' : 'text.disabled',
            fontStyle: customer ? 'normal' : 'italic',
            fontWeight: 400,
            lineHeight: 1.2,
          }}
        >
          {customer ?? t('projects.noCustomer')}
        </Typography>
      )}
    </Box>
  );
}
