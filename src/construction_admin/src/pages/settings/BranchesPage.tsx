import { AccountTreeOutlined, AddOutlined, DeleteOutlined, EditOutlined, PeopleOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
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
import { BranchDot } from '../../features/branches/BranchDot';
import { BranchFormDialog } from '../../features/branches/BranchFormDialog';
import { BranchEmployeesDialog } from '../../features/branches/BranchEmployeesDialog';
import { BranchProjectsDialog } from '../../features/branches/BranchProjectsDialog';
import { useBranchesQuery, useDeleteBranch } from '../../features/branches/useBranches';
import { countryLabel } from '../../data/countries';
import { useT } from '../../i18n/useI18n';

/** Business units (poslovne jedinice) of the operator's own organisation. Admin and above. */
export function BranchesPanel() {
  const t = useT();
  const { data: branches, isLoading, isError, error, refetch } = useBranchesQuery();
  const deleteBranch = useDeleteBranch();

  const [editing, setEditing] = useState<Branch | 'new' | null>(null);
  const [toDelete, setToDelete] = useState<Branch | null>(null);
  const [assigning, setAssigning] = useState<Branch | null>(null);
  const [assigningPeople, setAssigningPeople] = useState<Branch | null>(null);

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;

  return (
    <>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        useFlexGap
        sx={{ mb: 2, justifyContent: 'space-between', alignItems: { xs: 'flex-start', sm: 'center' } }}
      >
        <Box sx={{ maxWidth: 640 }}>
          <Typography variant="body2" color="text.secondary">
            {t('branches.description')}
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddOutlined />} onClick={() => setEditing('new')}>
          {t('branches.add')}
        </Button>
      </Stack>

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
                <TableCell>{t('branches.employees')}</TableCell>
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
                  <TableCell>{branch.employeeCount}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={branch.isActive ? 'success' : 'default'}
                      label={branch.isActive ? t('branches.active') : t('branches.inactive')}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title={t('branches.assignEmployees')}>
                      <IconButton size="small" onClick={() => setAssigningPeople(branch)}>
                        <PeopleOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
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
      <BranchEmployeesDialog branch={assigningPeople} onClose={() => setAssigningPeople(null)} />

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
