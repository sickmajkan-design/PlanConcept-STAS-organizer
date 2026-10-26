import { Box, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';

import { useProjectCustomers } from '../features/projects/useProjectCustomers';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

interface ProjectLabelProps {
  name: string;
  projectId?: string | null;
  /** Known already (the screen's own data carries it), so no lookup is needed. */
  customerName?: string | null;
  customerId?: string | null;
  /** `inline`: the customer sits beside the name. `stacked`: under it, for narrow places. */
  variant?: 'inline' | 'stacked';
  /** Name is rendered in a single line with an ellipsis when the place is tight. */
  noWrap?: boolean;
}

/**
 * The project name with its customer next to it. Use this wherever a project name is shown.
 * Clicking the customer opens the projects of that customer.
 */
export function ProjectLabel({ name, projectId, customerName, customerId, variant = 'inline', noWrap }: ProjectLabelProps) {
  const t = useT();
  const navigate = useNavigate();
  const { data } = useProjectCustomers();

  const found = data ? (projectId && data.byId.get(projectId)) || data.byName.get(name) : undefined;
  const known = customerName !== undefined || !!found;
  const customer = customerName !== undefined ? customerName : found?.name ?? null;
  const id = customerId !== undefined ? customerId : found?.id ?? null;

  const stacked = variant === 'stacked';
  const tagSx = {
    ml: stacked ? 0 : 0.75,
    color: customer ? 'secondary.main' : 'text.disabled',
    fontStyle: customer ? 'normal' : 'italic',
    fontWeight: 400,
    lineHeight: 1.2,
  } as const;

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
      {known &&
        (customer && id ? (
          <Typography
            component="a"
            href={`${paths.projects}?customer=${id}`}
            variant="caption"
            noWrap={noWrap}
            title={t('projects.showCustomer', { name: customer })}
            onClick={(event: React.MouseEvent) => {
              // The label usually sits inside a card or a row that opens something of its own.
              event.preventDefault();
              event.stopPropagation();
              navigate(`${paths.projects}?customer=${id}`);
            }}
            sx={{ ...tagSx, textDecoration: 'none', '&:hover': { textDecoration: 'underline' } }}
          >
            {customer}
          </Typography>
        ) : (
          <Typography component="span" variant="caption" noWrap={noWrap} sx={tagSx}>
            {customer ?? t('projects.noCustomer')}
          </Typography>
        ))}
    </Box>
  );
}
