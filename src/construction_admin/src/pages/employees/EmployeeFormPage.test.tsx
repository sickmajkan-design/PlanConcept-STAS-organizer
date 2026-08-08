/**
 * @vitest-environment jsdom
 */
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, type FakeNetwork } from '../../test/renderScreen';

/**
 * The employee form, rendered whole.
 *
 * This is the first screen test in the suite, and it is this screen because a
 * CRUD form is where a regression is both likely and silent: validation that
 * stops running still looks like a working form until the API refuses the
 * save, and a field error that lands on the wrong input looks like a form that
 * rejects correct data for no stated reason.
 *
 * The page is imported dynamically inside the test rather than at the top, so
 * that `renderScreen` — which installs the fake adapter on `axios.defaults`
 * when it initialises — is guaranteed to have run first. `axios.create` copies
 * the adapter at creation time, and the API client is created the moment the
 * page's import chain is evaluated.
 */
let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

async function renderForm(route = '/employees/new', path = '/employees/new') {
  const { EmployeeFormPage } = await import('./EmployeeFormPage');

  return renderScreen(<EmployeeFormPage />, { route, path });
}

/**
 * The form's submit control.
 *
 * By type rather than by label: the button reads "Create employee" when new
 * and "Save changes" when editing, in two languages, and a test that asserted
 * a label would be asserting the wording rather than the behaviour. There is
 * exactly one submit button on the form.
 */
function submitButton(): HTMLButtonElement {
  const button = document.querySelector<HTMLButtonElement>('form button[type="submit"]');

  if (!button) {
    throw new Error('The form has no submit button.');
  }

  return button;
}

/** Requests the screen made that actually change something. */
function writes() {
  return network.calls.filter((call) => call.method !== 'GET');
}

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

const saved = {
  id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  employeeNumber: 'EMP-001',
  firstName: 'Ivan',
  lastName: 'Horvat',
  fullName: 'Ivan Horvat',
  phone: null,
  email: null,
  address: null,
  dateOfBirth: null,
  employmentDate: '2024-01-15',
  position: 'Zidar',
  status: 'Active',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

describe('EmployeeFormPage', () => {
  it('refuses to submit an empty form and sends nothing', async () => {
    // The property that matters is the second half. A form that shows errors
    // but still posts is worse than one that does neither, because the API
    // then has to be the only thing standing between a typo and a record.
    const user = userEvent.setup();
    await renderForm();

    await user.click(submitButton());

    await waitFor(() => {
      expect(screen.getAllByText(/is required/i).length).toBeGreaterThan(0);
    });

    expect(writes()).toHaveLength(0);
  }, SCREEN_TIMEOUT);

  it('sends what was typed, trimmed, with blanks as null', async () => {
    // The mapping in onSubmit: trims the identifiers, and turns an untouched
    // optional field into null rather than an empty string. An empty string
    // reaches the database as an empty string, and then "has no phone number"
    // and "has a blank phone number" are two different rows.
    const user = userEvent.setup();
    network.reply('/employees', 200, saved);

    await renderForm();

    await user.type(screen.getByLabelText(/broj radnika|employee number/i), '  EMP-001  ');
    await user.type(screen.getByLabelText(/pozicija|position/i), ' Zidar ');
    await user.type(screen.getByLabelText(/^ime|first name/i), ' Ivan ');
    await user.type(screen.getByLabelText(/prezime|last name/i), ' Horvat ');
    await user.type(screen.getByLabelText(/datum zaposlenja|employment date/i), '2024-01-15');

    await user.click(submitButton());

    await waitFor(() => expect(writes()).toHaveLength(1));

    const body = writes()[0].body as Record<string, unknown>;

    expect(writes()[0].method).toBe('POST');
    expect(body.employeeNumber).toBe('EMP-001');
    expect(body.firstName).toBe('Ivan');
    expect(body.position).toBe('Zidar');
    expect(body.phone).toBeNull();
    expect(body.email).toBeNull();
    expect(body.address).toBeNull();
  }, SCREEN_TIMEOUT);

  it('puts a server-side field error on the field that caused it', async () => {
    // The subtle one. The API reports names in PascalCase, the form fields are
    // camelCase, and the handler lowercases the first letter to bridge them.
    // Break that and the operator gets a banner saying something is wrong with
    // no indication which of ten inputs it is.
    const user = userEvent.setup();

    network.reply('/employees', 400, {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { EmployeeNumber: ['Employee number is already in use.'] },
    });

    await renderForm();

    await user.type(screen.getByLabelText(/broj radnika|employee number/i), 'EMP-001');
    await user.type(screen.getByLabelText(/pozicija|position/i), 'Zidar');
    await user.type(screen.getByLabelText(/^ime|first name/i), 'Ivan');
    await user.type(screen.getByLabelText(/prezime|last name/i), 'Horvat');
    await user.type(screen.getByLabelText(/datum zaposlenja|employment date/i), '2024-01-15');

    await user.click(submitButton());

    const field = await screen.findByLabelText(/broj radnika|employee number/i);

    await waitFor(() => {
      expect(field.getAttribute('aria-invalid')).toBe('true');
    });

    // Beside the input, not only in the banner.
    expect(screen.getAllByText(/already in use/i).length).toBeGreaterThan(0);
  }, SCREEN_TIMEOUT);

  it('shows a conflict in the banner, since the API sends no field for it', async () => {
    // Worth pinning because it is the most common failure this form has, and
    // it does *not* go through the field-mapping path above: the API answers a
    // duplicate employee number with a plain 409 whose problem-details body
    // carries only `detail` — `ValidationProblemDetails` with an `errors`
    // dictionary is the 400 case. So the message has to reach the operator
    // through the banner or not at all.
    const user = userEvent.setup();

    network.reply('/employees', 409, {
      title: 'Conflict',
      status: 409,
      detail: "Employee number 'EMP-001' is already in use.",
    });

    await renderForm();

    await user.type(screen.getByLabelText(/broj radnika|employee number/i), 'EMP-001');
    await user.type(screen.getByLabelText(/pozicija|position/i), 'Zidar');
    await user.type(screen.getByLabelText(/^ime|first name/i), 'Ivan');
    await user.type(screen.getByLabelText(/prezime|last name/i), 'Horvat');
    await user.type(screen.getByLabelText(/datum zaposlenja|employment date/i), '2024-01-15');

    await user.click(submitButton());

    expect(await screen.findByText(/already in use/i)).toBeDefined();
  }, SCREEN_TIMEOUT);

  it('rejects a date of birth after the employment date', async () => {
    // A cross-field rule, which is the kind that quietly stops working when a
    // schema is refactored: each field is individually valid.
    const user = userEvent.setup();
    await renderForm();

    await user.type(screen.getByLabelText(/broj radnika|employee number/i), 'EMP-002');
    await user.type(screen.getByLabelText(/pozicija|position/i), 'Zidar');
    await user.type(screen.getByLabelText(/^ime|first name/i), 'Ana');
    await user.type(screen.getByLabelText(/prezime|last name/i), 'Marić');
    await user.type(screen.getByLabelText(/datum zaposlenja|employment date/i), '2020-01-01');
    await user.type(screen.getByLabelText(/datum rođenja|date of birth/i), '2021-06-01');

    await user.click(submitButton());

    await waitFor(() => {
      expect(screen.getByText(/before the employment date/i)).toBeDefined();
    });

    expect(writes()).toHaveLength(0);
  }, SCREEN_TIMEOUT);

  it('rejects an address that is not an email address', async () => {
    const user = userEvent.setup();
    await renderForm();

    await user.type(screen.getByLabelText(/broj radnika|employee number/i), 'EMP-003');
    await user.type(screen.getByLabelText(/pozicija|position/i), 'Zidar');
    await user.type(screen.getByLabelText(/^ime|first name/i), 'Ana');
    await user.type(screen.getByLabelText(/prezime|last name/i), 'Marić');
    await user.type(screen.getByLabelText(/datum zaposlenja|employment date/i), '2020-01-01');
    await user.type(screen.getByLabelText(/e-?pošta|email/i), 'ana(at)example.test');

    await user.click(submitButton());

    await waitFor(() => {
      expect(screen.getByText(/not a valid email address/i)).toBeDefined();
    });

    expect(writes()).toHaveLength(0);
  }, SCREEN_TIMEOUT);

  it('loads an existing employee into the form when editing', async () => {
    // Without this the edit screen opens blank and a save wipes the record —
    // the most expensive silent failure a CRUD form has.
    network.reply('/employees/', 200, { ...saved, phone: '+381 60 111 2233' });

    await renderForm(`/employees/${saved.id}/edit`, '/employees/:id/edit');

    const number = await screen.findByLabelText(/broj radnika|employee number/i);

    await waitFor(() => expect((number as HTMLInputElement).value).toBe('EMP-001'));

    expect(
      (screen.getByLabelText(/telefon|phone/i) as HTMLInputElement).value,
    ).toBe('+381 60 111 2233');
  }, SCREEN_TIMEOUT);

  it('updates rather than creates when editing', async () => {
    // Same form, different verb. Getting this wrong creates a duplicate on
    // every save and leaves the original untouched.
    const user = userEvent.setup();

    network.reply('/employees/', 200, saved);
    network.reply('/employees/', 200, saved);

    await renderForm(`/employees/${saved.id}/edit`, '/employees/:id/edit');

    const number = await screen.findByLabelText(/broj radnika|employee number/i);
    await waitFor(() => expect((number as HTMLInputElement).value).toBe('EMP-001'));

    await user.click(submitButton());

    await waitFor(() => expect(writes()).toHaveLength(1));

    expect(writes()[0].method).toBe('PUT');
    expect(writes()[0].url).toContain(saved.id);
  }, SCREEN_TIMEOUT);
});
