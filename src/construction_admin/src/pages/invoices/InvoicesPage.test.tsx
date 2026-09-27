/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * Invoices are money for management: only Admin/Super Admin with the finance grant see and use
 * this page. Recorded, not issued. An invoice on a client with companies must name them and add
 * up to the whole.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function invoice(overrides: Record<string, unknown> = {}) {
  return {
    id: 'i1',
    number: 'R-100',
    projectId: 'p1',
    projectName: 'Gradiliste Alfa',
    customerId: 'c1',
    customerName: 'Klijent d.o.o.',
    issueDate: '2026-09-20',
    dueDate: null,
    description: null,
    amount: 5000,
    payrollYear: 2026,
    payrollMonth: 9,
    status: 'Issued',
    cancelReason: null,
    createdAt: '2026-09-20T08:00:00Z',
    shares: [{ customerCompanyId: null, companyName: null, amount: 5000 }],
    ...overrides,
  };
}

function page(items: unknown[]) {
  return { items, pageNumber: 1, pageSize: 100, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

const manager = () => ({ ...signedIn('Admin'), financeAccess: 'Full' as const });

async function renderInvoices(user = manager()) {
  const { InvoicesPage } = await import('./InvoicesPage');

  return renderScreen(<InvoicesPage />, { route: '/', path: '/', user });
}

describe('InvoicesPage', () => {
  it('shows the invoice, its site and client, amount and status', async () => {
    network.reply('/invoices', 200, page([invoice()]));

    await renderInvoices();

    expect(await screen.findByText('R-100')).toBeDefined();
    expect(screen.getByText(/Gradiliste Alfa/)).toBeDefined();
    expect(screen.getByText(/Klijent d\.o\.o\./)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('offers Mark paid and Cancel only for an issued invoice', async () => {
    network.reply('/invoices', 200, page([invoice(), invoice({ id: 'i2', number: 'R-101', status: 'Paid' })]));

    await renderInvoices();
    await screen.findByText('R-100');

    expect(screen.getAllByRole('button', { name: 'Mark paid' })).toHaveLength(1);
    expect(screen.getAllByRole('button', { name: 'Cancel invoice' })).toHaveLength(1);
  }, SCREEN_TIMEOUT);

  it('names the companies on a split invoice, or says whole client', async () => {
    network.reply(
      '/invoices',
      200,
      page([
        invoice({ shares: [{ customerCompanyId: null, companyName: null, amount: 5000 }] }),
        invoice({
          id: 'i2',
          number: 'R-101',
          shares: [
            { customerCompanyId: 'co1', companyName: 'Firma A', amount: 2000 },
            { customerCompanyId: 'co2', companyName: 'Firma B', amount: 3000 },
          ],
        }),
      ]),
    );

    await renderInvoices();

    expect(await screen.findByText('Whole client')).toBeDefined();
    expect(screen.getByText('Firma A +1')).toBeDefined();
  }, SCREEN_TIMEOUT);
});

describe('NewInvoiceDialog', () => {
  it('splits evenly to the cent among the chosen companies', async () => {
    network.reply('/invoices', 200, page([]));
    network.reply('/projects', 200, { items: [{ id: 'p1', name: 'Gradiliste Alfa', customerId: 'c1', customerName: 'Klijent' }] });
    network.reply('/customers/c1/companies', 200, [
      { id: 'co1', customerId: 'c1', name: 'Firma A', address: null, isActive: true, invoiceCount: 0 },
      { id: 'co2', customerId: 'c1', name: 'Firma B', address: null, isActive: true, invoiceCount: 0 },
      { id: 'co3', customerId: 'c1', name: 'Firma C', address: null, isActive: true, invoiceCount: 0 },
    ]);
    network.reply('/invoices', 201, invoice({ id: 'i9' }));

    await renderInvoices();
    await userEvent.click(await screen.findByRole('button', { name: /New invoice/ }));

    await userEvent.click(screen.getByLabelText('Site'));
    await userEvent.click(await screen.findByText(/Gradiliste Alfa/));

    await screen.findByText('Firma A');
    await userEvent.type(screen.getByLabelText('Invoice number'), 'R-200');
    await userEvent.type(screen.getByLabelText(/Amount/), '100');

    await userEvent.click(screen.getByLabelText('Firma A'));
    await userEvent.click(screen.getByLabelText('Firma B'));
    await userEvent.click(screen.getByLabelText('Firma C'));

    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.includes('/invoices'));
    expect(posts).toHaveLength(1);
    const body = posts[0]?.body as { companyIds: string[] };
    expect(body.companyIds).toEqual(['co1', 'co2', 'co3']);
  }, SCREEN_TIMEOUT);

  it('refuses to save typed parts that do not add up to the invoice amount', async () => {
    network.reply('/invoices', 200, page([]));
    network.reply('/projects', 200, { items: [{ id: 'p1', name: 'Gradiliste Alfa', customerId: 'c1', customerName: 'Klijent' }] });
    network.reply('/customers/c1/companies', 200, [
      { id: 'co1', customerId: 'c1', name: 'Firma A', address: null, isActive: true, invoiceCount: 0 },
      { id: 'co2', customerId: 'c1', name: 'Firma B', address: null, isActive: true, invoiceCount: 0 },
    ]);

    await renderInvoices();
    await userEvent.click(await screen.findByRole('button', { name: /New invoice/ }));

    await userEvent.click(screen.getByLabelText('Site'));
    await userEvent.click(await screen.findByText(/Gradiliste Alfa/));
    await screen.findByText('Firma A');

    await userEvent.type(screen.getByLabelText('Invoice number'), 'R-201');
    await userEvent.type(screen.getByLabelText(/Amount/), '1000');
    await userEvent.click(screen.getByLabelText('Firma A'));
    await userEvent.click(screen.getByLabelText('Firma B'));
    await userEvent.click(screen.getByLabelText('Split evenly'));

    const partFields = screen.getAllByLabelText('Part (EUR)');
    await userEvent.type(partFields[0]!, '600');

    expect((screen.getByRole('button', { name: 'Save changes' }) as HTMLButtonElement).disabled).toBe(true);

    const posts = network.calls.filter((call) => call.method === 'POST' && call.url.includes('/invoices'));
    expect(posts).toHaveLength(0);
  }, SCREEN_TIMEOUT);
});
