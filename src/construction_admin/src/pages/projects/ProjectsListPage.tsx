import {
  AddOutlined,
  ApartmentOutlined,
  DeleteOutlined,
  EditOutlined,
  SubdirectoryArrowRightOutlined,
} from '@mui/icons-material';
import {
  Box,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { ProjectListQuery } from '../../api/projects';
import type { Project, ProjectStatus } from '../../api/types';
import { projectStatuses } from '../../api/types';
import { exportsApi } from '../../api/exports';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ExportButton } from '../../components/ExportButton';
import { ErrorState } from '../../components/ErrorState';
import { EmptyState } from '../../components/EmptyState';
import { PageHeader } from '../../components/PageHeader';
import { RowActions } from '../../components/RowActions';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import { useAllCustomersQuery } from '../../features/customers/useCustomers';
import { useDeleteProject, useProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { paths } from '../../routes/paths';
import { formatDate } from '../../utils/formatting';

interface ProjectViewState {
  search: string;
  filter: ProjectStatus | '';
  customerFilter: string;
}

// A construction office's whole project list, grouped by customer, fits
// comfortably in one page-worth — the same "board, not a paged grid" call
// made for the Assignment Board and Work Time.
const FULL_LIST: { pageNumber: number; pageSize: number } = { pageNumber: 1, pageSize: 500 };

interface ProjectBlock {
  main: Project | null;
  /** Set only when `main` is null — the parent exists but was filtered out of this result set. */
  orphanParentName?: string;
  subs: Project[];
}

interface CustomerGroup {
  key: string;
  customerName: string;
  projectCount: number;
  blocks: ProjectBlock[];
}

export function ProjectsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState<ProjectStatus>('name');
  const [customerFilter, setCustomerFilter] = useState('');
  const { data: customers } = useAllCustomersQuery();

  const savedViews = useSavedViews<ProjectViewState>('projects');

  const applyView = (state: ProjectViewState) => {
    list.setSearch(state.search);
    list.setFilter(state.filter);
    setCustomerFilter(state.customerFilter);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, { search: list.search, filter: list.filter, customerFilter });
  };

  const query: ProjectListQuery = useMemo(
    () => ({
      ...list.query,
      ...FULL_LIST,
      status: list.filter || undefined,
      customerId: customerFilter || undefined,
    }),
    [list.query, list.filter, customerFilter],
  );

  const { data, isLoading, isError, error, refetch } = useProjectsQuery(query);
  const remove = useDeleteWithConfirm<Project>(useDeleteProject());

  const groups = useMemo<CustomerGroup[]>(() => {
    const items = data?.items ?? [];
    const unassignedLabel = t('projects.noCustomer');

    const byCustomer = new Map<string, { customerName: string; projects: Project[] }>();
    for (const project of items) {
      const key = project.customerId ?? '';
      const bucket = byCustomer.get(key);
      if (bucket) {
        bucket.projects.push(project);
      } else {
        byCustomer.set(key, {
          customerName: project.customerName ?? unassignedLabel,
          projects: [project],
        });
      }
    }

    const result: CustomerGroup[] = [...byCustomer.entries()].map(([key, bucket]) => {
      const subsByParent = new Map<string, Project[]>();
      const nestedSubIds = new Set<string>();

      for (const project of bucket.projects) {
        if (project.kind === 'Sub' && project.parentProjectId) {
          const siblings = subsByParent.get(project.parentProjectId) ?? [];
          siblings.push(project);
          subsByParent.set(project.parentProjectId, siblings);
        }
      }

      const blocks: ProjectBlock[] = bucket.projects
        .filter((project) => project.kind === 'Main')
        .sort((a, b) => a.name.localeCompare(b.name))
        .map((main) => {
          const subs = (subsByParent.get(main.id) ?? []).sort((a, b) =>
            a.name.localeCompare(b.name),
          );
          subs.forEach((sub) => nestedSubIds.add(sub.id));
          return { main, subs };
        });

      // A sub-project whose Main was filtered out of this result set (a
      // different status, say) still needs somewhere to show — on its own,
      // naming the parent it belongs to instead of vanishing silently.
      bucket.projects
        .filter((project) => project.kind === 'Sub' && !nestedSubIds.has(project.id))
        .forEach((sub) => {
          blocks.push({ main: null, orphanParentName: sub.parentProjectName ?? undefined, subs: [sub] });
        });

      return { key, customerName: bucket.customerName, projectCount: bucket.projects.length, blocks };
    });

    result.sort((a, b) => {
      if (a.customerName === unassignedLabel) return 1;
      if (b.customerName === unassignedLabel) return -1;
      return a.customerName.localeCompare(b.customerName);
    });

    return result;
  }, [data, t]);

  return (
    <Box>
      <PageHeader
        title={t('projects.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('projects.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.projectNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('projects.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="project-status-filter-label">{t('projects.status')}</InputLabel>
          <Select
            labelId="project-status-filter-label"
            label={t('projects.status')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as ProjectStatus | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {projectStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('projectStatus', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <StatusLegend kind="projectStatus" values={projectStatuses} />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="project-customer-filter-label">{t('projects.customer')}</InputLabel>
          <Select
            labelId="project-customer-filter-label"
            label={t('projects.customer')}
            value={customerFilter}
            onChange={(event) => {
              setCustomerFilter(event.target.value);
              list.resetToFirstPage();
            }}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {(customers?.items ?? []).map((customer) => (
              <MenuItem key={customer.id} value={customer.id}>
                {customer.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <ExportButton
          onExport={(language) =>
            exportsApi.projects({ search: list.search, status: list.filter, language })
          }
        />
      </Stack>

      <Box sx={{ mb: 2 }}>
        <SavedViewsBar
          views={savedViews.views}
          onApply={applyView}
          onSave={saveCurrentView}
          onDelete={savedViews.deleteView}
        />
      </Box>

      {isError ? (
        <ErrorState error={error} onRetry={() => void refetch()} />
      ) : isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : groups.length === 0 ? (
        <EmptyState message={t('common.noResults')} />
      ) : (
        <Stack spacing={3}>
          {groups.map((group) => (
            <Box key={group.key}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {group.customerName}
                </Typography>
                <Chip size="small" variant="outlined" label={group.projectCount} />
              </Stack>

              <Paper variant="outlined">
                {group.blocks.map((block, index) => (
                  <Box key={block.main?.id ?? `orphan-${index}`}>
                    {index > 0 && <Divider />}

                    {block.main ? (
                      <ProjectRow
                        project={block.main}
                        isMain
                        onOpen={() => navigate(paths.projectDetail(block.main!.id))}
                        onAddSub={() => navigate(paths.projectNewSub(block.main!.id))}
                        onEdit={() => navigate(paths.projectEdit(block.main!.id))}
                        onDelete={() => remove.request(block.main!)}
                      />
                    ) : (
                      block.orphanParentName && (
                        <Box sx={{ px: 2, pt: 1 }}>
                          <Typography variant="caption" color="text.secondary">
                            {t('projects.subOf', { name: block.orphanParentName })}
                          </Typography>
                        </Box>
                      )
                    )}

                    {block.subs.map((sub) => (
                      <ProjectRow
                        key={sub.id}
                        project={sub}
                        isMain={false}
                        onOpen={() => navigate(paths.projectDetail(sub.id))}
                        onEdit={() => navigate(paths.projectEdit(sub.id))}
                        onDelete={() => remove.request(sub)}
                      />
                    ))}
                  </Box>
                ))}
              </Paper>
            </Box>
          ))}
        </Stack>
      )}

      <ConfirmDialog
        open={!!remove.pending}
        title={t('projects.deleteTitle')}
        description={
          remove.pending
            ? t('projects.deleteBody', { name: remove.pending.name })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

/**
 * One project's row — a Main project sits flush left with a building icon; a
 * Sub-project is indented under it with a corner-arrow icon and a tinted
 * background, so which is which reads at a glance without a separate column.
 */
function ProjectRow({
  project,
  isMain,
  onOpen,
  onAddSub,
  onEdit,
  onDelete,
}: {
  project: Project;
  isMain: boolean;
  onOpen: () => void;
  onAddSub?: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const t = useT();

  return (
    <Stack
      direction="row"
      spacing={1.5}
      onClick={onOpen}
      sx={{
        alignItems: 'center',
        py: 1,
        pl: isMain ? 2 : 5,
        pr: 1.5,
        cursor: 'pointer',
        bgcolor: isMain ? 'transparent' : 'action.hover',
        '&:hover': { bgcolor: 'action.selected' },
      }}
    >
      {isMain ? (
        <ApartmentOutlined fontSize="small" color="primary" />
      ) : (
        <SubdirectoryArrowRightOutlined fontSize="small" color="disabled" />
      )}

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: isMain ? 700 : 500 }} noWrap>
          {project.name}
        </Typography>
        {project.address && (
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {project.address}
          </Typography>
        )}
      </Box>

      <StatusChip status={project.status} kind="projectStatus" size="small" />

      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ width: 56, textAlign: 'right', display: { xs: 'none', md: 'block' } }}
      >
        {project.employeeCount || '—'}
      </Typography>

      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ width: 96, display: { xs: 'none', sm: 'block' } }}
      >
        {project.startDate ? formatDate(project.startDate) : '—'}
      </Typography>

      <RowActions>
        {isMain && onAddSub && (
          <Tooltip title={t('projects.addSubProject')}>
            <IconButton
              size="small"
              onClick={(event) => {
                event.stopPropagation();
                onAddSub();
              }}
            >
              <AddOutlined fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
        <Tooltip title={t('common.edit')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onEdit();
            }}
          >
            <EditOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title={t('common.delete')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onDelete();
            }}
          >
            <DeleteOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      </RowActions>
    </Stack>
  );
}
