import { AccountTreeOutlined, AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Branch, BranchInput } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { BranchDot } from '../../features/branches/BranchDot';
import { BranchProjectsDialog } from '../../features/branches/BranchProjectsDialog';
import {
  useBranchesQuery,
  useCreateBranch,
  useDeleteBranch,
  useUpdateBranch,
} from '../../features/branches/useBranches';
import { useT } from '../../i18n/useI18n';

/** The palette offered for a unit's dot; any #RRGGBB the API accepts, these are just the quick picks. */
const SWATCHES = ['#3457D5', '#0F8A5F', '#C2410C', '#7C3AED', '#0E7490', '#BE185D', '#4D7C0F', '#525252'];

const emptyInput: BranchInput = { name: '', color: SWATCHES[0], isActive: true };

/** Business units (poslovne jedinice) of the operator's own organisation. Admin and above. */
export function BranchesPage() {
  const t = useT();
  const { data: branches, isLoading, isError, error, refetch } = useBranchesQuery();
  const createBranch = useCreateBranch();
  const updateBranch = useUpdateBranch();
  const deleteBranch = useDeleteBranch();

  const [editing, setEditing] = useState<{ id?: string; input: BranchInput } | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [toDelete, setToDelete] = useState<Branch | null>(null);
  const [assigning, setAssigning] = useState<Branch | null>(null);

  const open = (branch?: Branch) => {
    setFormError(null);
    setEditing(
      branch
        ? { id: branch.id, input: { name: branch.name, color: branch.color, isActive: branch.isActive } }
        : { input: emptyInput },
    );
  };

  const save = async () => {
    if (!editing) return;
    const input = { ...editing.input, name: editing.input.name.trim() };

    if (!input.name) {
      setFormError(t('validation.required'));
      return;
    }

    try {
      if (editing.id) await updateBranch.mutateAsync({ id: editing.id, input });
      else await createBranch.mutateAsync(input);
      setEditing(null);
    } catch (e) {
      setFormError(toApiError(e).message);
    }
  };

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;

  return (
    <>
      <PageHeader
        title={t('branches.title')}
        description={t('branches.description')}
        action={{ label: t('branches.add'), icon: <AddOutlined />, onClick: () => open() }}
      />

      {!isLoading && branches?.length === 0 ? (
        <EmptyState message={t('branches.empty')} />
      ) : (
        <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>{t('branches.name')}</TableCell>
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
                      <span>{branch.name}</span>
                    </Stack>
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
                      <IconButton size="small" onClick={() => open(branch)}>
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

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>{editing?.id ? t('branches.edit') : t('branches.add')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField
              label={t('branches.name')}
              value={editing?.input.name ?? ''}
              onChange={(e) => setEditing((s) => s && { ...s, input: { ...s.input, name: e.target.value } })}
              autoFocus
              fullWidth
              slotProps={{ htmlInput: { maxLength: 200 } }}
            />
            <Box>
              <Box sx={{ mb: 1, fontSize: 13, color: 'text.secondary' }}>{t('branches.color')}</Box>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {SWATCHES.map((color) => (
                  <IconButton
                    key={color}
                    aria-label={color}
                    aria-pressed={editing?.input.color === color}
                    onClick={() => setEditing((s) => s && { ...s, input: { ...s.input, color } })}
                    sx={{
                      border: 2,
                      borderColor: editing?.input.color === color ? 'text.primary' : 'transparent',
                    }}
                  >
                    <BranchDot color={color} size={16} />
                  </IconButton>
                ))}
              </Stack>
            </Box>
            <FormControlLabel
              control={
                <Switch
                  checked={editing?.input.isActive ?? true}
                  onChange={(e) =>
                    setEditing((s) => s && { ...s, input: { ...s.input, isActive: e.target.checked } })
                  }
                />
              }
              label={t('branches.active')}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditing(null)}>{t('common.cancel')}</Button>
          <Button
            variant="contained"
            onClick={() => void save()}
            disabled={createBranch.isPending || updateBranch.isPending}
          >
            {t('common.save')}
          </Button>
        </DialogActions>
      </Dialog>

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
