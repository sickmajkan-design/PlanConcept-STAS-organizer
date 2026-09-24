/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';
import { HomePage } from '../../pages/HomePage';

let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

/**
 * The dashboard is Admin/SuperAdmin only — every other role keeps the plain
 * static home page (`HomePage`'s own fallback branch, unchanged by this
 * feature). What matters here is the fork: the right role sees widgets, the
 * wrong one never even calls the layout endpoint.
 */
describe('HomePage dashboard', () => {
  it('shows the default widget catalog the API returns for a first-time layout', async () => {
    // The backend (not this screen) is responsible for synthesizing the
    // default catalog when a user has no saved layout — see
    // GetDashboardLayoutQueryHandler. This screen only has to render
    // whatever the endpoint sends back.
    network.reply('/dashboard-layout', 200, {
      widgets: [
        { id: '1', type: 'ProjectsRealization', column: 0, order: 0 },
        { id: '2', type: 'AbsencesBalance', column: 0, order: 1 },
        { id: '3', type: 'NotificationsBulletin', column: 0, order: 2 },
        { id: '4', type: 'DocumentExpiry', column: 0, order: 3 },
        { id: '5', type: 'FleetStatus', column: 1, order: 0 },
      ],
    });

    renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

    await waitFor(() => {
      expect(screen.getByText('Projects — realization')).toBeDefined();
    });

    expect(screen.getByText('Workforce')).toBeDefined();
    expect(screen.getByText('Notifications & bulletin')).toBeDefined();
    expect(screen.getByText('Documents expiring soon')).toBeDefined();
    expect(screen.getByText('Vehicles & tools')).toBeDefined();
  });

  it('never mounts the dashboard for a role below Admin', async () => {
    renderScreen(<HomePage />, { user: signedIn('Foreman') });

    await waitFor(() => {
      expect(screen.getByText('Pick a section from the menu to get started.')).toBeDefined();
    });

    expect(screen.queryByRole('button', { name: 'Add widget' })).toBeNull();
    expect(network.calls.some((call) => call.url.includes('/dashboard-layout'))).toBe(false);
  });
  describe('costs', () => {
    const layout = {
      widgets: [
        { id: '1', type: 'CompanyKpi', column: 0, order: 0 },
        { id: '2', type: 'CostTrend', column: 1, order: 0 },
      ],
    };

    const emptyPage = { items: [], totalCount: 0, pageNumber: 1, pageSize: 1, totalPages: 0 };

    beforeEach(() => {
      network.reply('/vehicles', 200, emptyPage);
      network.reply('/tools', 200, emptyPage);
    });

    it('shows no cost figure or chart when no cost has been recorded', async () => {
      network.reply('/dashboard-layout', 200, layout);
      network.reply('/costs/company', 200, {
        from: '2026-01-01', to: '2026-01-31', includesLabour: true, unpricedMinutes: 0,
        labour: 0, manualPay: 0, material: 0, generalExpenses: 0, accommodation: 0, vehicles: 0, tools: 0, total: 0,
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('No costs recorded in the last months.')).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.queryByText('Cost this month')).toBeNull();
    });

    it('shows the cost tile once there is a cost to show', async () => {
      network.reply('/dashboard-layout', 200, layout);
      network.reply('/costs/company', 200, {
        from: '2026-01-01', to: '2026-01-31', includesLabour: true, unpricedMinutes: 0,
        labour: 0, manualPay: 0, material: 0, generalExpenses: 0, accommodation: 0, vehicles: 100, tools: 0, total: 100,
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Cost this month')).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.queryByText('No costs recorded in the last months.')).toBeNull();
    });

    it('does not draw or fetch any cost figure for an Admin without the finance right', async () => {
      network.reply('/dashboard-layout', 200, layout);

      renderScreen(<HomePage />, { user: signedIn('Admin') });

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Add widget' })).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.queryByText('Cost this month')).toBeNull();
      expect(screen.queryByText('No costs recorded in the last months.')).toBeNull();
      expect(network.calls.some((call) => call.url.includes('/costs/company'))).toBe(false);
    });

    it('draws them for an Admin once the finance right is granted', async () => {
      network.reply('/dashboard-layout', 200, layout);
      network.reply('/costs/company', 200, {
        from: '2026-01-01', to: '2026-01-31', includesLabour: true, unpricedMinutes: 0,
        labour: 0, manualPay: 0, material: 0, generalExpenses: 0, accommodation: 0, vehicles: 100, tools: 0, total: 100,
      });

      renderScreen(<HomePage />, { user: { ...signedIn('Admin'), financeAccess: 'Full' } });

      await waitFor(() => {
        expect(screen.getByText('Cost this month')).toBeDefined();
      }, { timeout: 5000 });
    });
  });

  describe('finance widgets', () => {
    const layout = { widgets: [{ id: '1', type: 'FinanceOverview', column: 0, order: 0 }] };
    const totals = (revenue: number, expense: number) => ({
      from: '2026-09-01', to: '2026-09-30', revenue, expense, revenueProject: revenue, revenueOther: 0,
      profit: revenue - expense, marginPercent: revenue > 0 ? 20 : null,
    });

    it('shows income, spending and profit against the previous period', async () => {
      network.reply('/dashboard-layout', 200, layout);
      network.reply('/finance/series', 200, {
        granularity: 'Day', includesLabour: true, buckets: [],
        totals: totals(1000, 800), previous: totals(500, 800),
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Income')).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.getByText('Spending')).toBeDefined();
      expect(screen.getByText('Profit')).toBeDefined();
      // Income doubled: a rise, in green territory, not "infinite".
      expect(screen.getByText(/▲ 100.0%/)).toBeDefined();
    });

    it('says so when the period holds no money at all', async () => {
      network.reply('/dashboard-layout', 200, layout);
      network.reply('/finance/series', 200, {
        granularity: 'Day', includesLabour: true, buckets: [],
        totals: totals(0, 0), previous: totals(0, 0),
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('No data for the selected period.')).toBeDefined();
      }, { timeout: 5000 });
    });

    it('never asks the server for finance figures on behalf of an Admin without the right', async () => {
      network.reply('/dashboard-layout', 200, layout);

      renderScreen(<HomePage />, { user: signedIn('Admin') });

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Add widget' })).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.queryByText('Income')).toBeNull();
      expect(network.calls.some((call) => call.url.includes('/finance/series'))).toBe(false);
    });
  });

  describe('per-project finance widgets', () => {
    const rows = [
      { projectId: 'p1', projectName: 'Most', contractValue: null, budget: null, revenue: 3000, expense: 1000, profit: 2000, marginPercent: 66.7 },
      { projectId: 'p2', projectName: 'Loser', contractValue: null, budget: null, revenue: 0, expense: 500, profit: -500, marginPercent: null },
    ];
    const money = (revenue: number, expense: number) => ({ revenue, expense, profit: revenue - expense });
    const byProject = {
      from: '2026-09-01', to: '2026-09-30', includesLabour: true, rows, totalProjects: 2,
      company: money(3400, 1620), unallocated: money(400, 120), unassignedPayOverlaps: 0, housingDoubleEntries: 0,
    };

    it('lists projects by spending with the largest first', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'TopProjectsByExpense', column: 0, order: 0 }] });
      network.reply('/finance/by-project', 200, byProject);

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Most')).toBeDefined();
      }, { timeout: 5000 });
      expect(screen.getByText('Loser')).toBeDefined();
    });

    it('starts the profit table with the project losing the most and shows a dash for a missing margin', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'ProfitByProject', column: 0, order: 0 }] });
      network.reply('/finance/by-project', 200, byProject);

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Loser')).toBeDefined();
      }, { timeout: 5000 });

      const names = screen.getAllByRole('row').map((row) => row.textContent ?? '');
      // Header first, then the loss, then the earner.
      expect(names[1]).toContain('Loser');
      expect(names[1]).toContain('—');
      expect(names[2]).toContain('Most');
    });

    it('accounts for everything: listed projects, the ones left off, and what no project owns add up to the company', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'ProfitByProject', column: 0, order: 0 }] });
      network.reply('/finance/by-project', 200, {
        ...byProject,
        totalProjects: 3,
        // A third project (500 in, 100 out) is beyond the list; the company holds all of it.
        company: money(3900, 1720),
        unassignedPayOverlaps: 2,
        housingDoubleEntries: 3,
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Other projects (1)')).toBeDefined();
      }, { timeout: 5000 });

      const text = screen.getAllByRole('row').map((row) => row.textContent ?? '');
      const other = text.find((row) => row.startsWith('Other projects'))!;
      expect(other).toContain('500.00');
      expect(other).toContain('100.00');
      expect(text.some((row) => row.startsWith('Company total') && row.includes('3,900.00') && row.includes('1,720.00'))).toBe(true);
      expect(screen.getByText(/2 pay entries tied to no project/)).toBeDefined();
      expect(screen.getByText(/3 housing expenses fall on days/)).toBeDefined();
    });

    it('is not offered to an Admin without the right', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'ProfitByProject', column: 0, order: 0 }] });

      renderScreen(<HomePage />, { user: signedIn('Admin') });

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Add widget' })).toBeDefined();
      }, { timeout: 5000 });
      expect(network.calls.some((call) => call.url.includes('/finance/by-project'))).toBe(false);
    });
  });

  describe('widgets with settings', () => {
    const PROJECT_ID = '22222222-2222-2222-2222-222222222222';
    const money = (revenue: number, expense: number) => ({
      revenue, expense, profit: revenue - expense, marginPercent: revenue > 0 ? Math.round(((revenue - expense) / revenue) * 1000) / 10 : null,
    });
    const projectsPage = {
      items: [{ id: PROJECT_ID, name: 'Rezidencija', kind: 'Main', status: 'Active' }],
      totalCount: 1, pageNumber: 1, pageSize: 1000, totalPages: 1,
    };

    it('shows what the money goes on, with the pieces adding up to the total shown above them', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'CostBreakdown', x: 0, y: 0, w: 6, h: 12 }] });
      network.reply('/projects', 200, projectsPage);
      network.reply('/finance/breakdown', 200, {
        from: '2026-09-01', to: '2026-09-30', projectId: null, includesLabour: true, total: 1000,
        items: [
          { kind: 'Labour', amount: 600 }, { kind: 'ManualPay', amount: 0 }, { kind: 'Material', amount: 400 },
          { kind: 'GeneralExpenses', amount: 0 }, { kind: 'Accommodation', amount: 0 }, { kind: 'Vehicles', amount: 0 }, { kind: 'Tools', amount: 0 },
        ],
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('1,000.00')).toBeDefined();
      }, { timeout: 5000 });

      // Whole company: no project on the request.
      const call = network.calls.find((c) => c.url.includes('/finance/breakdown'))!;
      expect(call.params.projectId).toBeUndefined();
    });

    it('asks for one project only when a project is chosen in its settings', async () => {
      network.reply('/dashboard-layout', 200, {
        widgets: [{ id: '1', type: 'CostBreakdown', x: 0, y: 0, w: 6, h: 12, settings: { projectId: PROJECT_ID } }],
      });
      network.reply('/projects', 200, projectsPage);
      network.reply('/finance/breakdown', 200, {
        from: '2026-09-01', to: '2026-09-30', projectId: PROJECT_ID, includesLabour: true, total: 0,
        items: [{ kind: 'Labour', amount: 0 }],
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(network.calls.some((c) => c.url.includes('/finance/breakdown') && c.params.projectId === PROJECT_ID)).toBe(true);
      }, { timeout: 5000 });
    });

    it('asks which project to show and does not call the server until one is chosen', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'ProjectFocus', x: 0, y: 0, w: 6, h: 12 }] });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('Choose the project this widget shows.')).toBeDefined();
      }, { timeout: 5000 });

      expect(network.calls.some((c) => c.url.includes('/summary'))).toBe(false);
    });

    it('measures the budget against everything to date, and lets an overrun show past 100%', async () => {
      network.reply('/dashboard-layout', 200, {
        widgets: [{ id: '1', type: 'ProjectFocus', x: 0, y: 0, w: 6, h: 12, settings: { projectId: PROJECT_ID } }],
      });
      network.reply(`/finance/projects/${PROJECT_ID}/summary`, 200, {
        projectId: PROJECT_ID, projectName: 'Rezidencija', contractValue: 1000, budget: 400, includesLabour: true,
        period: money(250, 100), toDate: money(500, 480), toDateFrom: '2025-01-01',
        budgetUsedPercent: 120, contractCollectedPercent: 50,
      });

      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await waitFor(() => {
        expect(screen.getByText('120.0%')).toBeDefined();
      }, { timeout: 5000 });

      expect(screen.getByText('50.0%')).toBeDefined();
      expect(screen.getByText('480.00 / 400.00')).toBeDefined();
      expect(screen.getByText(/Project in focus — Rezidencija/)).toBeDefined();
    });

    it('keeps a widgets settings in the saved layout when they are changed', async () => {
      network.reply('/dashboard-layout', 200, { widgets: [{ id: '1', type: 'TopProjectsByExpense', x: 0, y: 0, w: 6, h: 12 }] });
      network.reply('/finance/by-project', 200, {
        from: '2026-09-01', to: '2026-09-30', includesLabour: true, rows: [], totalProjects: 0,
        company: money(0, 0), unallocated: money(0, 0), unassignedPayOverlaps: 0, housingDoubleEntries: 0,
      });

      const user = userEvent.setup();
      renderScreen(<HomePage />, { user: signedIn('SuperAdmin') });

      await user.click(await screen.findByRole('button', { name: 'Widget settings' }, { timeout: 5000 }));
      await user.click(await screen.findByRole('combobox', { name: 'Rows shown' }));
      await user.click(await screen.findByRole('option', { name: '10' }));
      await user.click(screen.getByRole('button', { name: 'Save changes' }));

      await waitFor(() => {
        const save = network.calls.find((c) => c.method === 'PUT' && c.url.includes('/dashboard-layout'));
        expect(save).toBeDefined();
        const widgets = (save!.body as { widgets: { settings?: Record<string, string> }[] }).widgets;
        expect(widgets[0]!.settings).toEqual({ top: '10' });
      }, { timeout: 5000 });

      // And the widget now asks for ten.
      expect(network.calls.some((c) => c.url.includes('/finance/by-project') && Number(c.params.top) === 10)).toBe(true);
    });
  });
});
