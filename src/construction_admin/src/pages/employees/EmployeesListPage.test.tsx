/**
 * @vitest-environment jsdom
 */
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';

/**
 * The employee list, rendered whole.
 *
 * The form test covers a screen that writes; this covers one that reads. The
 * failures are different in kind: a grid that renders no rows for data that
 * arrived, a search box whose term never reaches the query, a delete that
 * fires without the confirmation the operator thought they had.
 *
 * The grid is MUI's DataGrid, which virtualises. In jsdom every element has
 * zero height, so it renders the first page's rows into the DOM and that is
 * enough for these assertions — but it is why the fixtures below are small
 * rather than realistic.
 */
let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

/**
 * A longer timeout than the 5 s default, on the screen tests only.
 *
 * The first test in each file pays for the dynamic import of the page and its
 * whole dependency tree — MUI, the DataGrid, react-hook-form — which is a
 * second or two on an idle machine and more on a busy one. It timed out once
 * on a run competing with a type-check, which is a flake, and a flaky screen
 * test is worse than none: it teaches people to re-run rather than look.
 */
const SCREEN_TIMEOUT = 20_000;

function employee(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    employeeNumber: 'EMP-001',
    firstName: 'Ivan',
    lastName: 'Horvat',
    fullName: 'Ivan Horvat',
    phone: '+381 60 111 2233',
    email: 'ivan@example.test',
    address: null,
    dateOfBirth: null,
    employmentDate: '2024-01-15',
    position: 'Zidar',
    status: 'Active',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...overrides,
  };
}

function page(items: unknown[]) {
  return {
    items,
    pageNumber: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

async function renderList() {
  const { EmployeesListPage } = await import('./EmployeesListPage');

  return renderScreen(<EmployeesListPage />, { route: '/employees', path: '/employees' });
}

/** The list requests, newest last. */
function listCalls() {
  return network.calls.filter(
    (call) => call.method === 'GET' && call.url.includes('/employees'),
  );
}

describe('EmployeesListPage', () => {
  it('renders a row for each employee the API returned', async () => {
    // The failure this catches is a grid showing "no rows" for data that
    // arrived — a column definition renamed, a row id missing. It looks like
    // an empty database.
    network.reply('/employees', 200, page([
      employee(),
      employee({
        id: '22222222-2222-2222-2222-222222222222',
        employeeNumber: 'EMP-002',
        firstName: 'Ana',
        lastName: 'Marić',
        fullName: 'Ana Marić',
        position: 'Električar',
      }),
    ]));

    await renderList();

    expect(await screen.findByText('EMP-001')).toBeDefined();
    expect(await screen.findByText('EMP-002')).toBeDefined();
    expect(screen.getByText('Ana Marić')).toBeDefined();
    expect(screen.getByText('Električar')).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('sends the typed search term to the API', async () => {
    // Debounced, so the assertion waits rather than checking immediately. A
    // search box whose term never reaches the query looks like a search that
    // finds nothing.
    const user = userEvent.setup();

    network.reply('/employees', 200, page([employee()]));

    await renderList();
    await screen.findByText('EMP-001');

    const search = screen.getByRole('textbox');
    await user.type(search, 'Horvat');

    await waitFor(
      () => {
        const last = listCalls().at(-1);
        expect(last?.params.search).toBe('Horvat');
      },
      { timeout: 3000 },
    );
  }, SCREEN_TIMEOUT);

  it('asks before deleting, and sends nothing if the operator backs out', async () => {
    // The half that is easy to lose in a refactor: the dialog still appears,
    // but the delete has already been sent behind it.
    const user = userEvent.setup();

    network.reply('/employees', 200, page([employee()]));

    await renderList();
    await screen.findByText('EMP-001');

    await user.click(screen.getByRole('button', { name: /obriši|delete/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/EMP-001|Ivan Horvat/)).toBeDefined();

    expect(network.calls.some((call) => call.method === 'DELETE')).toBe(false);

    await user.click(within(dialog).getByRole('button', { name: /otkaži|cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());

    expect(network.calls.some((call) => call.method === 'DELETE')).toBe(false);
  }, SCREEN_TIMEOUT);

  it('deletes only after the confirmation is accepted', async () => {
    const user = userEvent.setup();

    network.reply('/employees', 200, page([employee()]));

    await renderList();
    await screen.findByText('EMP-001');

    await user.click(screen.getByRole('button', { name: /obriši|delete/i }));

    const dialog = await screen.findByRole('dialog');

    await user.click(
      within(dialog).getByRole('button', { name: /^(obriši|delete|potvrdi|confirm)/i }),
    );

    await waitFor(() => {
      const deletes = network.calls.filter((call) => call.method === 'DELETE');
      expect(deletes).toHaveLength(1);
      expect(deletes[0].url).toContain('11111111-1111-1111-1111-111111111111');
    });
  }, SCREEN_TIMEOUT);

  it('shows the failure rather than an empty grid when the list cannot load', async () => {
    // An empty grid says "there are no employees", which is a different and
    // much more alarming statement than "the request failed".
    // The body the API's own 500 handler sends: a title, no detail, and a
    // traceId. `toApiError` prefers the title over its generic fallback, so
    // the operator sees the server's words and can quote the trace.
    network.reply('/employees', 500, {
      title: 'An unexpected error occurred.',
      status: 500,
      traceId: '00-abc-def-01',
    });

    await renderList();

    expect(await screen.findByText(/unexpected error occurred/i)).toBeDefined();
  }, SCREEN_TIMEOUT);
});
