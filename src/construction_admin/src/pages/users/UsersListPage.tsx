import {
  AddOutlined,
  BlockOutlined,
  EditOutlined,
  LockOpenOutlined,
} from '@mui/icons-material';
import {
  Box,
  Chip,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { UserListQuery } from '../../api/users';
import type { Role, UserAccount } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import { useAuth } from '../../auth/useAuth';
import {
  useActivateUser,
  useDeactivateUser,
  useUsersQuery,
} from '../../features/users/useUsers';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { formatDate } from '../../utils/formatting';

/** Mirrors the API's RoleAdministration: everyone may act strictly below
 * themselves, and a Super Admin may also act on peers. Shown here only to keep
 * the UI honest — the API enforces it regardless of what the buttons do. */
const RANK: Record<Role, number> = {
  SuperAdmin: 1,
  Admin: 2,
  ProjectManager: 3,
  Foreman: 4,
  Worker: 5,
};

function canManage(callerRole: Role | undefined, targetRole: Role): boolean {
  if (!callerRole) {
    return false;
  }

  return callerRole === 'SuperAdmin' || RANK[callerRole] < RANK[targetRole];
}

export function UsersListPage() {
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState<Role>('isActive');
  const [pendingOffboard, setPendingOffboard] = useState<UserAccount | null>(null);

  const query: UserListQuery = useMemo(
    () => ({ ...list.query, role: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useUsersQuery(query);
  const deactivate = useDeactivateUser();
  const activate = useActivateUser();

  const actionError = deactivate.error ?? activate.error;

  const confirmOffboard = () => {
    if (!pendingOffboard) {
      return;
    }

    deactivate.mutate(pendingOffboard.id, {
      onSuccess: () => setPendingOffboard(null),
    });
  };

  const columns: GridColDef<UserAccount>[] = useMemo(
    () => [
      { field: 'email', headerName: t('users.email'), flex: 1, minWidth: 220 },
      {
        field: 'role',
        headerName: t('users.role'),
        width: 150,
        valueFormatter: (value: string) => enumLabel('role', value),
      },
      {
        field: 'employeeName',
        headerName: t('users.employee'),
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'isActive',
        headerName: t('users.access'),
        width: 130,
        renderCell: (params) =>
          params.row.isActive ? (
            <Chip size="small" color="success" variant="outlined" label={t('users.active')} />
          ) : (
            <Chip size="small" color="default" label={t('users.deactivated')} />
          ),
      },
      {
        field: 'lastLoginAt',
        headerName: t('users.lastSignIn'),
        width: 130,
        valueFormatter: (value: string | null) => formatDate(value),
      },
      {
        field: 'actions',
        headerName: '',
        width: 110,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const target = params.row;
          const allowed = canManage(currentUser?.role as Role | undefined, target.role);
          const isSelf = target.id === currentUser?.id;

          return (
            <Stack direction="row" spacing={0.5}>
              <Tooltip title={allowed ? t('common.edit') : t('users.cannotAdminister')}>
                <span>
                  <IconButton
                    size="small"
                    disabled={!allowed}
                    onClick={() => navigate(paths.userEdit(target.id))}
                  >
                    <EditOutlined fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>

              {target.isActive ? (
                <Tooltip
                  title={
                    isSelf
                      ? t('users.cannotRevokeSelf')
                      : allowed
                        ? t('users.revoke')
                        : t('users.cannotAdminister')
                  }
                >
                  <span>
                    <IconButton
                      size="small"
                      color="error"
                      disabled={!allowed || isSelf}
                      onClick={() => setPendingOffboard(target)}
                    >
                      <BlockOutlined fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              ) : (
                <Tooltip title={allowed ? t('users.restore') : t('users.cannotAdminister')}>
                  <span>
                    <IconButton
                      size="small"
                      disabled={!allowed}
                      onClick={() => activate.mutate(target.id)}
                    >
                      <LockOpenOutlined fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              )}
            </Stack>
          );
        },
      },
    ],
    [navigate, currentUser, activate, t, enumLabel],
  );

  return (
    <Box>
      <PageHeader
        title={t('users.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('users.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.userNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('users.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="user-role-filter-label">{t('users.role')}</InputLabel>
          <Select
            labelId="user-role-filter-label"
            label={t('users.role')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as Role | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {(Object.keys(RANK) as Role[]).map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('role', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={list.paginationModel}
        onPaginationModelChange={list.setPaginationModel}
        sortModel={list.sortModel}
        onSortModelChange={list.setSortModel}
      />

      <ConfirmDialog
        open={!!pendingOffboard}
        title={t('users.revokeTitle')}
        description={
          pendingOffboard
            ? t('users.revokeBody', { email: pendingOffboard.email })
            : ''
        }
        confirmLabel={t('users.revoke')}
        destructive
        loading={deactivate.isPending}
        onConfirm={confirmOffboard}
        onCancel={() => setPendingOffboard(null)}
      />

      {actionError && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {toApiError(actionError).message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}
