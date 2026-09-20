import { CloseOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  CircularProgress,
  Dialog,
  DialogContent,
  IconButton,
  Link,
  Stack,
  Typography,
} from '@mui/material';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import {
  CompositionBar,
  CompositionLegend,
  CostLine,
  CostSection,
  EmptyLine,
  Tile,
  numeric,
  type Segment,
} from '../../components/costs/costUi';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import type { Period } from './monthWindow';

export interface AssetLine {
  id: string;
  primary: ReactNode;
  secondary?: ReactNode;
  amount: string;
  meta?: ReactNode;
}

export interface AssetSection {
  key: string;
  title: string;
  color?: string;
  total?: string;
  share?: number;
  note?: string;
  lines: AssetLine[];
}

/**
 * The shared shape of a per-vehicle or per-tool cost dialog: what it cost in the
 * period and what that is made of, a row of facts, then every cost itemised.
 */
export function AssetCostDialog({
  open,
  onClose,
  period,
  title,
  linkTo,
  linkLabel,
  total,
  segments,
  tiles,
  sections,
  formatMoneyValue,
  loading,
  error,
}: {
  open: boolean;
  onClose: () => void;
  period: Period;
  title: string;
  linkTo?: string;
  linkLabel?: string;
  total: string;
  segments: Segment[];
  tiles: { label: string; value: string; hint?: string; tone?: 'warn' }[];
  sections: AssetSection[];
  formatMoneyValue: (value: number) => string;
  loading?: boolean;
  error?: string | null;
}) {
  const t = useT();

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md" scroll="paper">
      <Box sx={{ px: 3, pt: 2.5, pb: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="caption" color="text.secondary">
              {formatDate(period.from)} – {formatDate(period.to)}
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: -0.3 }}>
              {title}
            </Typography>
            {linkTo && (
              <Link component={RouterLink} to={linkTo} underline="hover" variant="body2" onClick={onClose}>
                {linkLabel}
              </Link>
            )}
          </Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
            <Box sx={{ textAlign: 'right' }}>
              <Typography variant="caption" color="text.secondary">
                {t('costs.total')}
              </Typography>
              <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: -0.5, lineHeight: 1.1, ...numeric }}>
                {total}
              </Typography>
            </Box>
            <IconButton onClick={onClose} aria-label={t('common.close')} sx={{ mr: -1 }}>
              <CloseOutlined />
            </IconButton>
          </Stack>
        </Stack>
      </Box>

      <DialogContent sx={{ px: 3, py: 2.5 }}>
        <Stack spacing={3.5}>
          <Stack spacing={1.25}>
            <CompositionBar segments={segments} height={12} />
            <CompositionLegend segments={segments} format={formatMoneyValue} showShare />
          </Stack>

          {tiles.length > 0 && (
            <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1.5 }}>
              {tiles.map((tile) => (
                <Tile key={tile.label} {...tile} />
              ))}
            </Stack>
          )}

          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
              <CircularProgress />
            </Box>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          {!loading &&
            sections
              .filter((section) => section.lines.length > 0 || section.note)
              .map((section) => (
              <CostSection
                key={section.key}
                title={section.title}
                color={section.color}
                total={section.total}
                share={section.share}
                note={section.note}
              >
                {section.lines.length === 0 ? (
                  <EmptyLine>{t('costs.noEntries')}</EmptyLine>
                ) : (
                  section.lines.map((line) => (
                    <CostLine
                      key={line.id}
                      primary={line.primary}
                      secondary={line.secondary}
                      amount={line.amount}
                      meta={line.meta}
                    />
                  ))
                )}
              </CostSection>
            ))}
        </Stack>
      </DialogContent>
    </Dialog>
  );
}
