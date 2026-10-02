import { AccountTreeOutlined, AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Chip,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import type { Branch } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { BranchDot } from '../../features/branches/BranchDot';
import { BranchFormDialog } from '../../features/branches/BranchFormDialog';
import { BranchProjectsDialog } from '../../features/branches/BranchProjectsDialog';
import { useBranchesQuery, useDeleteBranch } from '../../features/branches/useBranches';
import { countryLabel } from '../../data/countries';
import { useT } from '../../i18n/useI18n';

/** Business units (poslovne jedinice) of the operator's own organisation. Admin and above. */
export function BranchesPage() {
  const t = useT();
  const { data: branches, isLoading, isError, error, refetch } = useBranchesQuery();
  const deleteBranch = useDeleteBranch();

  const [editing, setEditing] = useState<Branch | 'new' | null>(null);
  const [toDelete, setToDelete] = useState<Branch | null>(null);
  const [assigning, setAssigning] = useState<Branch | null>(null);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;

  return (
    <>
      <PageHeader
        title={t('branches.title')}
        description={t('branches.description')}
        action={{ label: t('branches.add'), icon: <AddOutlined />, onClick: () => setEditing('new') }}
      />

      {!isLoading && branches?.length === 0 ? (
        <EmptyState message={t('branches.empty')} />
      ) : (
        <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>{t('branches.name')}</TableCell>
                <TableCell>{t('branches.kind')}</TableCell>
                <TableCell>{t('branches.place')}</TableCell>
                <TableCell>{t('branches.projects')}</TableCell>
                <TableCell>{t('branches.status')}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {(branches ?? []).map((branch) => (
                <TableRow key={branch.id} hover>
                  <TableCell>
                    <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
                      <BranchDot color={branch.color} />
                      <Stack>
                        <span>{branch.name}</span>
                        {branch.legalName && branch.legalName !== branch.name && (
                          <Typography variant="caption" color="text.secondary">
                            {branch.legalName}
                          </Typography>
                        )}
                      </Stack>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    {t(branch.kind === 'LegalEntity' ? 'branches.kindEntity' : 'branches.kindOffice')}
                  </TableCell>
                  <TableCell>
                    {[branch.city, countryLabel(branch.countryCode)].filter(Boolean).join(', ') || '—'}
                  </TableCell>
                  <TableCell>{branch.projectCount}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={branch.isActive ? 'success' : 'default'}
                      label={branch.isActive ? t('branches.active') : t('branches.inactive')}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title={t('branches.assignProjects')}>
                      <IconButton size="small" onClick={() => setAssigning(branch)}>
                        <AccountTreeOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title={t('common.edit')}>
                      <IconButton size="small" onClick={() => setEditing(branch)}>
                        <EditOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip
                      title={branch.projectCount > 0 ? t('branches.hasProjects') : t('common.delete')}
                    >
                      <span>
                        <IconButton
                          size="small"
                          color="error"
                          disabled={branch.projectCount > 0}
                          onClick={() => setToDelete(branch)}
                        >
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      <BranchProjectsDialog branch={assigning} onClose={() => setAssigning(null)} />

      <BranchFormDialog target={editing} onClose={() => setEditing(null)} />

      <ConfirmDialog
        open={!!toDelete}
        title={t('branches.deleteTitle')}
        description={t('branches.deleteDescription', { name: toDelete?.name ?? '' })}
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={async () => {
          if (toDelete) await deleteBranch.mutateAsync(toDelete.id);
          setToDelete(null);
        }}
        onCancel={() => setToDelete(null)}
      />
    </>
  );
}
