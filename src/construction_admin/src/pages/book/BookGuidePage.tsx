import {
  Box,
  Button,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

import { PageHeader } from '../../components/PageHeader';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const functions: { key: string; to: string }[] = [
  { key: 'overview', to: paths.bookOverview },
  { key: 'cashflow', to: paths.bookOverview },
  { key: 'forecast', to: paths.bookOverview },
  { key: 'quick', to: paths.bookEntries },
  { key: 'kinds', to: paths.bookEntries },
  { key: 'source', to: paths.bookEntries },
  { key: 'filters', to: paths.bookEntries },
  { key: 'receivables', to: paths.bookReceivables },
  { key: 'settlement', to: paths.bookSettlement },
  { key: 'billing', to: paths.bookSettlement },
  { key: 'payouts', to: paths.bookPayouts },
  { key: 'recurring', to: paths.bookRecurring },
  { key: 'categories', to: paths.bookCategories },
  { key: 'import', to: paths.bookImport },
  { key: 'checks', to: paths.bookSettlement },
  { key: 'lock', to: paths.bookSettlement },
];

/**
 * The first screen of the book: what it is for, how a month goes, and what each function does, with a link to
 * where it is done. The same explanations stand on the screens themselves, under "How this works".
 */
export function BookGuidePage() {
  const t = useT();

  return (
    <Stack spacing={3}>
      <PageHeader title={t('book.guide.title')} description={t('book.guide.intro')} />

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        {(
          [
            ['one', paths.bookOverview],
            ['two', paths.bookEntries],
            ['three', paths.bookSettlement],
          ] as const
        ).map(([key, to], index) => (
          <Paper key={key} variant="outlined" sx={{ p: 2, flex: 1 }}>
            <Typography variant="caption" color="text.secondary">
              {t('book.guide.step', { number: index + 1 })}
            </Typography>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t(`book.guide.step.${key}.title` as MessageKey)}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
              {t(`book.guide.step.${key}.text` as MessageKey)}
            </Typography>
            <Button size="small" variant="outlined" component={RouterLink} to={to}>
              {t('book.guide.open')}
            </Button>
          </Paper>
        ))}
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.guide.idea')}
        </Typography>
        <Typography variant="body1" sx={{ mt: 1 }}>
          {t('book.guide.ideaText')}
        </Typography>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.guide.month')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {t('book.guide.monthHelp')}
        </Typography>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
          {(['enter', 'compute', 'check', 'lock', 'export'] as const).map((key, index) => (
            <Box key={key} sx={{ flex: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {index + 1}. {t(`book.guide.month.${key}.title` as MessageKey)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t(`book.guide.month.${key}.text` as MessageKey)}
              </Typography>
            </Box>
          ))}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.guide.functions')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('book.guide.functionsHelp')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.guide.col.function')}</TableCell>
                <TableCell>{t('book.guide.col.what')}</TableCell>
                <TableCell>{t('book.guide.col.example')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {functions.map((item) => (
                <TableRow key={item.key} hover>
                  <TableCell sx={{ fontWeight: 600 }}>
                    {t(`book.guide.fn.${item.key}.name` as MessageKey)}
                  </TableCell>
                  <TableCell>{t(`book.guide.fn.${item.key}.what` as MessageKey)}</TableCell>
                  <TableCell>{t(`book.guide.fn.${item.key}.example` as MessageKey)}</TableCell>
                  <TableCell align="right">
                    <Button size="small" component={RouterLink} to={item.to}>
                      {t('book.guide.open')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2, bgcolor: 'action.hover' }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.guide.not')}
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {t('book.guide.notText')}
        </Typography>
      </Paper>
    </Stack>
  );
}
