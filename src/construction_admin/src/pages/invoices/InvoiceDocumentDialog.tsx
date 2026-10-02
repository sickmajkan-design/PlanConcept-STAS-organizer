import { CloseOutlined, PrintOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  Divider,
  GlobalStyles,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from '@mui/material';

import type { Invoice, InvoiceIssuer } from '../../api/invoices';
import { countryLabel } from '../../data/countries';
import { useInvoiceDocumentQuery } from '../../features/invoices/useInvoices';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

/** What a sheet of paper should be when this dialog is printed: only the document, on the page. */
const PRINT_STYLES = {
  '@media print': {
    '#root': { display: 'none !important' },
    '.MuiDialog-root, .MuiDialog-container, .MuiDialog-paper': {
      position: 'static !important',
      overflow: 'visible !important',
      height: 'auto !important',
      maxHeight: 'none !important',
      boxShadow: 'none !important',
      '--Paper-shadow': 'none !important',
    },
    '.MuiPaper-root.MuiDialog-paper': { boxShadow: 'none !important', backgroundImage: 'none !important' },
    '.MuiBackdrop-root': { display: 'none !important' },
    '.no-print': { display: 'none !important' },
    '@page': { margin: '14mm' },
  },
} as const;

function IssuerBlock({ issuer }: { issuer: InvoiceIssuer }) {
  const { t } = useI18n();
  const place = [issuer.postalCode, issuer.city].filter(Boolean).join(' ');
  const lines = [issuer.address, place, issuer.countryCode ? countryLabel(issuer.countryCode) : null].filter(Boolean);

  const numbers: [string, string | null][] = [
    [t('customers.taxId'), issuer.taxId],
    [t('customers.registrationNumber'), issuer.registrationNumber],
    [t('customers.vatNumber'), issuer.vatNumber],
  ];

  return (
    <Box>
      <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.25 }}>
        {issuer.name || '—'}
      </Typography>
      {issuer.branchName && issuer.branchName !== issuer.name && (
        <Typography variant="body2" color="text.secondary">
          {issuer.branchName}
        </Typography>
      )}
      {lines.map((line) => (
        <Typography key={line} variant="body2">
          {line}
        </Typography>
      ))}
      {numbers
        .filter(([, value]) => value)
        .map(([label, value]) => (
          <Typography key={label} variant="body2">
            {label}: {value}
          </Typography>
        ))}
      {issuer.usesCompanyNumbers && (
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
          {t('invoices.document.companyNumbers')}
        </Typography>
      )}
      {issuer.phone && <Typography variant="body2">{issuer.phone}</Typography>}
      {issuer.email && <Typography variant="body2">{issuer.email}</Typography>}
    </Box>
  );
}

/**
 * A printable copy of one recorded invoice: the issuing business unit's details at the top, then
 * the client, the work and the amounts. The program records invoices rather than issuing them, so
 * this is an extract of the record, and it says so at the foot.
 */
export function InvoiceDocumentDialog({ invoice, onClose }: { invoice: Invoice | null; onClose: () => void }) {
  const { t, locale } = useI18n();
  const enumLabel = useEnumLabel();
  const { data, isLoading, isError } = useInvoiceDocumentQuery(invoice?.id);

  return (
    <Dialog open={!!invoice} onClose={onClose} fullScreen>
      {invoice && <GlobalStyles styles={PRINT_STYLES} />}

      <Toolbar className="no-print" sx={{ gap: 1, borderBottom: 1, borderColor: 'divider' }}>
        <Typography variant="h6" sx={{ flex: 1 }}>
          {t('invoices.document.title')} {invoice?.number}
        </Typography>
        <Button variant="contained" startIcon={<PrintOutlined />} onClick={() => window.print()} disabled={!data}>
          {t('invoices.document.print')}
        </Button>
        <IconButton onClick={onClose} aria-label={t('common.close')}>
          <CloseOutlined />
        </IconButton>
      </Toolbar>

      <Box
        sx={{
          flex: 1,
          overflow: 'auto',
          bgcolor: { xs: 'background.paper', sm: 'action.hover' },
          py: { xs: 2, sm: 4 },
          '@media print': { bgcolor: 'common.white !important', py: '0 !important', overflow: 'visible' },
        }}
      >
        <Box
          sx={{
            maxWidth: 820,
            mx: 'auto',
            bgcolor: 'background.paper',
            p: { xs: 2, sm: 5 },
            boxShadow: { sm: 2 },
            '@media print': { boxShadow: 'none !important', p: '0 !important', maxWidth: 'none !important' },
          }}
        >
          {isLoading && (
            <Stack sx={{ alignItems: 'center', py: 6 }}>
              <CircularProgress />
            </Stack>
          )}
          {isError && <Alert severity="error">{t('invoices.document.failed')}</Alert>}

          {data && (
            <Stack spacing={3}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} sx={{ justifyContent: 'space-between' }}>
                <IssuerBlock issuer={data.issuer} />
                <Box sx={{ textAlign: { sm: 'right' } }}>
                  <Typography variant="overline" color="text.secondary">
                    {t('invoices.document.title')}
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 700 }}>
                    {data.number}
                  </Typography>
                  <Typography variant="body2">
                    {t('invoices.issueDate')}: {formatDate(data.issueDate)}
                  </Typography>
                  {data.dueDate && (
                    <Typography variant="body2">
                      {t('invoices.dueDate')}: {formatDate(data.dueDate)}
                    </Typography>
                  )}
                  <Typography variant="body2">
                    {t('invoices.statusColumn')}: {enumLabel('invoiceStatus', data.status)}
                  </Typography>
                  {data.cancelReason && (
                    <Typography variant="body2" color="error">
                      {data.cancelReason}
                    </Typography>
                  )}
                </Box>
              </Stack>

              <Divider />

              <Box>
                <Typography variant="overline" color="text.secondary">
                  {t('invoices.document.client')}
                </Typography>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {data.customerName ?? '—'}
                </Typography>
                {data.customerContactPerson && <Typography variant="body2">{data.customerContactPerson}</Typography>}
              </Box>

              <Box>
                <Typography variant="overline" color="text.secondary">
                  {t('invoices.project')}
                </Typography>
                <Typography variant="body1">{data.projectName}</Typography>
                {data.projectAddress && <Typography variant="body2">{data.projectAddress}</Typography>}
                {data.description && (
                  <Typography variant="body2" sx={{ mt: 1, whiteSpace: 'pre-wrap' }}>
                    {data.description}
                  </Typography>
                )}
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                  {t('invoices.payrollMonth')}: {String(data.payrollMonth).padStart(2, '0')}.{data.payrollYear}
                </Typography>
              </Box>

              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('invoices.document.recipient')}</TableCell>
                    <TableCell align="right">{t('invoices.amount')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.recipients.map((recipient, index) => {
                    const numbers = [
                      recipient.taxId && `${t('customers.taxId')}: ${recipient.taxId}`,
                      recipient.registrationNumber && `${t('customers.registrationNumber')}: ${recipient.registrationNumber}`,
                      recipient.vatNumber && `${t('customers.vatNumber')}: ${recipient.vatNumber}`,
                    ].filter(Boolean);

                    return (
                      <TableRow key={`${recipient.name}-${index}`}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {recipient.name || '—'}
                          </Typography>
                          {recipient.address && <Typography variant="caption">{recipient.address}</Typography>}
                          {numbers.length > 0 && (
                            <Typography variant="caption" sx={{ display: 'block' }}>
                              {numbers.join(' · ')}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell align="right">{formatMoney(recipient.amount, locale)}</TableCell>
                      </TableRow>
                    );
                  })}
                  <TableRow>
                    <TableCell sx={{ fontWeight: 700 }}>{t('invoices.document.total')}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700 }}>
                      {formatMoney(data.amount, locale)}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>

              <Typography variant="caption" color="text.secondary">
                {t('invoices.document.notice')}
              </Typography>
            </Stack>
          )}
        </Box>
      </Box>
    </Dialog>
  );
}
