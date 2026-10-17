/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { GridColDef } from '@mui/x-data-grid';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn } from '../test/renderScreen';

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  installFakeNetwork();
});

interface Row {
  id: string;
  employeeNumber: string;
  fullName: string;
  position: string;
  branchName: string;
}

const columns: GridColDef<Row>[] = [
  { field: 'employeeNumber', headerName: 'Number', width: 120 },
  { field: 'fullName', headerName: 'Full name', width: 160 },
  { field: 'position', headerName: 'Position', width: 140 },
  { field: 'branchName', headerName: 'Business unit', width: 160 },
];

const grid = (over: Record<string, unknown> = {}) =>
  async function render() {
    const { ResourceDataGrid } = await import('./ResourceDataGrid');
    const rows: Row[] = (over.rows as Row[] | undefined) ?? [];

    return renderScreen(
      <ResourceDataGrid<Row>
        data={{ items: rows, totalCount: rows.length, pageNumber: 1, pageSize: 20, totalPages: 1, hasNextPage: false, hasPreviousPage: false }}
        columns={columns}
        isLoading={false}
        isError={false}
        error={null}
        onRetry={() => {}}
        paginationModel={{ page: 0, pageSize: 20 }}
        onPaginationModelChange={() => {}}
        sortModel={[]}
        onSortModelChange={() => {}}
        {...over}
      />,
      { route: '/', path: '/', user: signedIn('Admin') },
    );
  };

describe('ResourceDataGrid', () => {
  it('when nothing has been entered yet, says so and offers the one thing to do', async () => {
    const add = vi.fn();

    await grid({ empty: { title: 'No employees yet', action: { label: 'Add an employee', onClick: add } } })();

    expect(await screen.findByText('No employees yet')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Add an employee' }));
    expect(add).toHaveBeenCalledOnce();
  }, SCREEN_TIMEOUT);

  it('when a filter is on, says nothing matches and offers to clear it, not to add', async () => {
    const clear = vi.fn();

    await grid({ filtered: true, onClearFilters: clear, empty: { title: 'No employees yet', action: { label: 'Add an employee', onClick: vi.fn() } } })();

    expect(await screen.findByText('Nothing matches the chosen filters')).toBeDefined();
    expect(screen.queryByText('No employees yet')).toBeNull();
    await userEvent.click(screen.getByRole('button', { name: 'Clear filters' }));
    expect(clear).toHaveBeenCalledOnce();
  }, SCREEN_TIMEOUT);

  it('shows the important columns first on a narrow screen, and lets a person add the rest and keeps the choice', async () => {
    const rows = [{ id: 'a', employeeNumber: 'E1', fullName: 'Ana Novak', position: 'Mason', branchName: 'Zagreb' }];

    await grid({ rows, columnsKey: 'employees' })();

    // Narrow by default in the test browser: the unit is behind the Columns button.
    expect(await screen.findByText('Ana Novak')).toBeDefined();
    expect(screen.queryByText('Zagreb')).toBeNull();

    await userEvent.click(screen.getByRole('button', { name: 'Columns' }));
    await userEvent.click(await screen.findByRole('menuitem', { name: 'Business unit' }));

    await waitFor(() => expect(screen.getByText('Zagreb')).toBeDefined());
    expect(JSON.parse(window.localStorage.getItem('grid.columns.employees') ?? '{}').branchName).toBe(true);
  }, SCREEN_TIMEOUT);
});
