import { Box, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';

import { useProjectCustomers } from '../features/projects/useProjectCustomers';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { CountryFlag } from './CountryFlag';

interface ProjectLabelProps {
  name: string;
  projectId?: string | null;
  /** Known already (the screen's own data carries it), so no lookup is needed. */
  customerName?: string | null;
  customerId?: string | null;
  /** The customer's country (two letters). */
  customerCountryCode?: string | null;
  /** The country of the site; shown only when it differs from the customer's. */
  siteCountryCode?: string | null;
  /** `inline`: the customer sits beside the name. `stacked`: under it, for narrow places. */
  variant?: 'inline' | 'stacked';
  /** Name is rendered in a single line with an ellipsis when the place is tight. */
  noWrap?: boolean;
}

/**
 * The project name with its customer next to it, and the customer's country as a flag. Use this wherever a project
 * name is shown. Clicking the customer opens the projects of that customer.
 */
export function ProjectLabel({
  name,
  projectId,
  customerName,
  customerId,
  customerCountryCode,
  siteCountryCode,
  variant = 'inline',
  noWrap,
}: ProjectLabelProps) {
  const t = useT();
  const navigate = useNavigate();
  const { data } = useProjectCustomers();

  const found = data ? (projectId && data.byId.get(projectId)) || data.byName.get(name) : undefined;
  const known = customerName !== undefined || !!found;
  const customer = customerName !== undefined ? customerName : found?.name ?? null;
  const id = customerId !== undefined ? customerId : found?.id ?? null;
  const country = customerCountryCode !== undefined ? customerCountryCode : found?.countryCode ?? null;
  const site = siteCountryCode !== undefined ? siteCountryCode : found?.siteCountryCode ?? null;

  const stacked = variant === 'stacked';
  const tagSx = {
    ml: stacked ? 0 : 0.75,
    color: customer ? 'secondary.main' : 'text.disabled',
    fontStyle: customer ? 'normal' : 'italic',
    fontWeight: 400,
    lineHeight: 1.2,
  } as const;

  // The flag belongs to the customer, so it is not shown for a project that has none.
  const flag = customer && country ? <CountryFlag code={country} /> : null;
  const differs = !!customer && !!country && !!site && site.toUpperCase() !== country.toUpperCase();

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
            sx={{ ...tagSx, display: 'inline-flex', alignItems: 'center', gap: 0.5, textDecoration: 'none', '&:hover': { textDecoration: 'underline' } }}
          >
            {flag}
            {customer}
          </Typography>
        ) : (
          <Typography component="span" variant="caption" noWrap={noWrap} sx={{ ...tagSx, display: 'inline-flex', alignItems: 'center', gap: 0.5 }}>
            {flag}
            {customer ?? t('projects.noCustomer')}
          </Typography>
        ))}
      {differs && site && (
        <Typography
          component="span"
          variant="caption"
          color="text.secondary"
          sx={{ ml: stacked ? 0 : 0.75, mt: stacked ? 0.25 : 0, px: 0.5, border: '1px dashed', borderColor: 'divider', borderRadius: 1, width: 'fit-content', whiteSpace: 'nowrap' }}
        >
          {t('projectLabel.site')} <CountryFlag code={site} />
        </Typography>
      )}
    </Box>
  );
}
