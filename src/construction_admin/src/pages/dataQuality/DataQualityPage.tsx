import { CheckCircleOutlined } from '@mui/icons-material';
import { Alert, Box, Chip, CircularProgress, Link as MuiLink, Paper, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { dataQualityApi, type DataQualityGroup, type DataQualityItem } from '../../api/dataQuality';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

/** Where a record of each kind is fixed. Null leaves the line as plain text. */
const LINKS: Record<string, ((item: DataQualityItem) => string) | null> = {
  employeesNoPosition: (i) => paths.employeeEdit(i.id),
  vehiclesNoTd: (i) => paths.vehicleEdit(i.id),
  vehiclesNoDates: (i) => paths.vehicleEdit(i.id),
  projectsNoNeeds: () => paths.schedule,
  projectsNoDates: (i) => paths.projectEdit(i.id),
  postingsAfterEnd: (i) => paths.employeeDetail(i.id),
  unknownCards: () => paths.fuelReconciliation,
};

/** Most serious first: the ones that make another screen show something wrong. */
const ORDER = ['postingsAfterEnd', 'employeesNoPosition', 'projectsNoNeeds', 'projectsNoDates', 'vehiclesNoDates', 'vehiclesNoTd', 'unknownCards'];

function detailText(group: DataQualityGroup, item: DataQualityItem, t: ReturnType<typeof useT>): string | null {
  if (group.key === 'vehiclesNoDates' && item.detail) {
    return item.detail
      .split(',')
      .filter(Boolean)
      .map((part) => t(`dataQuality.missing.${part}` as MessageKey))
      .join(', ');
  }

  return item.detail;
}

/** The records that are incomplete or contradict themselves, with a link to fix each. */
export function DataQualityPage() {
  const t = useT();
  const { data, error, isLoading, refetch } = useQuery({ queryKey: ['dataQuality'], queryFn: dataQualityApi.get });

  if (error) return <ErrorState error={error} onRetry={() => void refetch()} />;

  if (isLoading || !data) {
    return (
      <Box sx={{ py: 8, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  const groups = [...data.groups].sort((a, b) => ORDER.indexOf(a.key) - ORDER.indexOf(b.key));
  const problems = groups.filter((g) => g.count > 0);
  const clean = groups.filter((g) => g.count === 0);

  return (
    <Box>
      <PageHeader title={t('dataQuality.title')} description={t('dataQuality.description')} />

      {problems.length === 0 && (
        <Alert icon={<CheckCircleOutlined />} severity="success">
          {t('dataQuality.allClean')}
        </Alert>
      )}

      <Stack spacing={2}>
        {problems.map((group) => {
          const link = LINKS[group.key];

          return (
            <Paper key={group.key} variant="outlined" sx={{ borderRadius: 2.5, p: 2 }}>
              <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 0.5 }}>
                <Typography variant="h6" sx={{ flex: 1 }}>
                  {t(`dataQuality.${group.key}.title` as MessageKey)}
                </Typography>
                <Chip color="warning" label={group.count} sx={{ fontWeight: 700 }} />
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5, maxWidth: '75ch' }}>
                {t(`dataQuality.${group.key}.why` as MessageKey)}
              </Typography>

              <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>
                {group.items.map((item, i) => {
                  const detail = detailText(group, item, t);
                  const body = (
                    <>
                      <Typography component="span" variant="body2" sx={{ fontWeight: 500 }}>
                        {item.label}
                      </Typography>
                      {detail && (
                        <Typography component="span" variant="body2" color="text.secondary">
                          {' · '}
                          {detail}
                        </Typography>
                      )}
                    </>
                  );

                  return (
                    <Box key={`${item.id}-${i}`} sx={{ py: 0.75 }}>
                      {link ? (
                        <MuiLink component={Link} to={link(item)} underline="hover" color="inherit">
                          {body}
                        </MuiLink>
                      ) : (
                        body
                      )}
                    </Box>
                  );
                })}
              </Stack>

              {group.count > group.items.length && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                  {t('dataQuality.more', { shown: group.items.length, total: group.count })}
                </Typography>
              )}
            </Paper>
          );
        })}
      </Stack>

      {problems.length > 0 && clean.length > 0 && (
        <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: 'wrap', mt: 3, alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {t('dataQuality.cleanChecks')}
          </Typography>
          {clean.map((g) => (
            <Chip key={g.key} size="small" variant="outlined" color="success" label={t(`dataQuality.${g.key}.title` as MessageKey)} />
          ))}
        </Stack>
      )}
    </Box>
  );
}
