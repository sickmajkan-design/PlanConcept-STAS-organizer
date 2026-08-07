import { AxiosError, AxiosHeaders } from 'axios';
import { describe, expect, it } from 'vitest';

import { ApiError, toApiError } from './apiError';

/** An Axios failure carrying a problem-details body, as the API sends them. */
function responseError(status: number, data?: unknown): AxiosError {
  const config = { headers: new AxiosHeaders() };

  return new AxiosError(
    `Request failed with status code ${status}`,
    String(status),
    config,
    {},
    {
      status,
      statusText: '',
      headers: {},
      config,
      data,
    } as never,
  );
}

/**
 * Every failure the operator sees comes through here. The thing worth pinning
 * down is the order it prefers messages in: the API's own explanation always
 * beats the generic fallback, because "The action conflicts with the current
 * data" tells nobody which employee number was already taken.
 */
describe('toApiError', () => {
  it('prefers a field message over anything more general', () => {
    const error = toApiError(
      responseError(400, {
        title: 'One or more validation errors occurred.',
        detail: 'See errors.',
        errors: { EmployeeNumber: ['That employee number is already in use.'] },
      }),
    );

    expect(error.message).toBe('That employee number is already in use.');
    expect(error.status).toBe(400);
    expect(error.isValidationError).toBe(true);
  });

  it('falls back to the API s detail, then its title', () => {
    expect(toApiError(responseError(409, { detail: 'Stock would go negative.' })).message)
      .toBe('Stock would go negative.');

    expect(toApiError(responseError(409, { title: 'Conflict happened.' })).message)
      .toBe('Conflict happened.');
  });

  it('has something to say for a bare status', () => {
    expect(toApiError(responseError(401)).message).toContain('session has expired');
    expect(toApiError(responseError(403)).message).toContain('permission');
    expect(toApiError(responseError(404)).message).toContain('could not be found');
    expect(toApiError(responseError(500)).message).toContain('server encountered an error');
    expect(toApiError(responseError(503)).message).toContain('server encountered an error');
  });

  it('distinguishes a timeout from an unreachable server', () => {
    const config = { headers: new AxiosHeaders() };

    const timedOut = toApiError(
      new AxiosError('timeout', AxiosError.ECONNABORTED, config),
    );

    expect(timedOut.message).toContain('took too long');

    const offline = toApiError(new AxiosError('Network Error', 'ERR_NETWORK', config));

    expect(offline.message).toContain('No connection');

    // Neither carries a status: there was no response to take one from, and a
    // screen that keys off `status === 500` must not treat these as one.
    expect(timedOut.status).toBeUndefined();
    expect(offline.status).toBeUndefined();
  });

  it('passes an ApiError through unchanged', () => {
    const original = new ApiError('Already translated.', 418);

    expect(toApiError(original)).toBe(original);
  });

  it('survives something that is not an error at all', () => {
    expect(toApiError('a string').message).toBe('Something went wrong.');
    expect(toApiError(undefined).message).toBe('Something went wrong.');
  });

  it('accepts a single string where the API sends one instead of a list', () => {
    const error = toApiError(
      responseError(400, { errors: { Email: 'Email is required.' } }),
    );

    expect(error.errorFor('email')).toBe('Email is required.');
  });
});

describe('ApiError.errorFor', () => {
  const error = new ApiError('Rejected.', 400, {
    EmployeeNumber: ['Already in use.'],
    Email: ['Not a valid address.', 'Also too long.'],
  });

  it('matches the form s camelCase against the API s PascalCase', () => {
    expect(error.errorFor('employeeNumber')).toBe('Already in use.');
  });

  it('shows only the first message for a field', () => {
    // A field can only display one line, and the first is the one the server
    // considered most important.
    expect(error.errorFor('email')).toBe('Not a valid address.');
  });

  it('says nothing about a field that was not rejected', () => {
    expect(error.errorFor('firstName')).toBeUndefined();
  });
});

describe('ApiError.isForbidden', () => {
  it('is true only for 403', () => {
    // The error panel hides its retry button on this, because retrying a
    // refusal is a button that can never work.
    expect(new ApiError('x', 403).isForbidden).toBe(true);
    expect(new ApiError('x', 401).isForbidden).toBe(false);
    expect(new ApiError('x').isForbidden).toBe(false);
  });
});
