/**
 * @vitest-environment jsdom
 */
import axios, {
  AxiosHeaders,
  type AxiosAdapter,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { sessionStore } from './session';

/**
 * What the live map asks the API for.
 *
 * The endpoint is paged now, and the map is the one screen where a truncated
 * response is invisible: a grid short of rows has a scrollbar that stops early,
 * a map short of markers just looks like a quieter day. So two things have to
 * hold — the request asks for the largest page the server will serve, and the
 * caller gets the envelope rather than a bare array, because the count is what
 * lets it notice.
 *
 * The adapter is installed on `axios.defaults` before `./client` is imported,
 * because `axios.create` copies the defaults at creation time — and the module
 * cache is reset between tests for the same reason: the instance created by the
 * first test would otherwise keep answering, and the later tests would record
 * their calls into the first test's array.
 */
type Call = { url: string; params: Record<string, unknown> };

function installFakeNetwork(body: unknown): Call[] {
  const calls: Call[] = [];

  const adapter: AxiosAdapter = (config) => {
    calls.push({
      url: config.url ?? '',
      params: (config.params ?? {}) as Record<string, unknown>,
    });

    const response: AxiosResponse = {
      data: body,
      status: 200,
      statusText: 'OK',
      headers: new AxiosHeaders(),
      config: config as InternalAxiosRequestConfig,
    };

    return Promise.resolve(response);
  };

  axios.defaults.adapter = adapter;
  vi.resetModules();
  return calls;
}

const page = {
  items: [
    {
      employeeId: '11111111-1111-1111-1111-111111111111',
      employeeNumber: 'EMP-1',
      fullName: 'Ivan Horvat',
      position: 'Site Manager',
      latitude: 45.81,
      longitude: 15.98,
      accuracy: 8,
      timestamp: '2026-08-05T12:00:00Z',
    },
  ],
  pageNumber: 1,
  pageSize: 1000,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('locationsApi.current', () => {
  beforeEach(() => {
    sessionStore.clear();
  });

  it('asks for the largest page the API will serve', async () => {
    const calls = installFakeNetwork(page);

    const { locationsApi, MAP_PAGE_SIZE } = await import('./locations');

    await locationsApi.current();

    expect(calls).toHaveLength(1);
    expect(calls[0].url).toBe('/api/v1/locations/current');
    expect(calls[0].params.pageSize).toBe(MAP_PAGE_SIZE);
  });

  it('returns the envelope, so a partial map can be spotted', async () => {
    // Not the bare array it used to return. Without totalCount the screen has
    // no way to tell "everyone is here" from "the first thousand are here".
    const calls = installFakeNetwork({ ...page, totalCount: 4000, hasNextPage: true });

    const { locationsApi } = await import('./locations');

    const result = await locationsApi.current();

    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(4000);
    expect(calls).toHaveLength(1);
  });

  it('still passes the filters that narrow it', async () => {
    // Narrowing by project is the remedy the truncation banner tells the
    // operator to reach for, so it has to survive the paging change.
    const calls = installFakeNetwork(page);

    const { locationsApi } = await import('./locations');

    await locationsApi.current({
      projectId: '22222222-2222-2222-2222-222222222222',
      maxAgeMinutes: 30,
      includeInactive: true,
    });

    expect(calls[0].params).toMatchObject({
      projectId: '22222222-2222-2222-2222-222222222222',
      maxAgeMinutes: 30,
      includeInactive: true,
    });
  });

  it('drops an empty project filter rather than sending it', async () => {
    // "All projects" is the empty string in the select. Sent through, it
    // would be a filter on nothing rather than no filter.
    const calls = installFakeNetwork(page);

    const { locationsApi } = await import('./locations');

    await locationsApi.current({ projectId: '' });

    expect(calls[0].params.projectId).toBeUndefined();
  });
});
