import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from './main'

const accounts = [
  { id: 1, name: 'Summit Auto Group', timeZone: 'America/Chicago' },
  { id: 20, name: 'Quiet Harbor Spa', timeZone: 'America/Los_Angeles' },
]

function summary(overrides: Record<string, unknown> = {}) {
  return {
    account: accounts[0],
    reportingWeek: { start: '2026-07-20', end: '2026-07-26' },
    baselineWeeks: [
      { start: '2026-07-13', end: '2026-07-19', total: 50, byEventType: { call_received: 31, lead_created: 12, appointment_set: 7 } },
      { start: '2026-07-06', end: '2026-07-12', total: 43, byEventType: { call_received: 25, lead_created: 11, appointment_set: 7 } },
      { start: '2026-06-29', end: '2026-07-05', total: 62, byEventType: { call_received: 40, lead_created: 14, appointment_set: 8 } },
      { start: '2026-06-22', end: '2026-06-28', total: 39, byEventType: { call_received: 24, lead_created: 9, appointment_set: 6 } },
    ],
    coverage: { status: 'complete', baselineWeeksRequired: 4, baselineWeeksAvailable: 4 },
    summary: { total: 53, baselineAverage: 48.5, percentageChange: 9.278, byEventType: { call_received: 33, lead_created: 13, appointment_set: 7 } },
    locations: [{ name: 'Downtown', total: 9, byEventType: { call_received: 5, lead_created: 3, appointment_set: 1 }, baseline: { weeklyTotals: [8, 10, 7, 9] } }],
    ...overrides,
  }
}

function mockApi(report: unknown = summary()) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input)
    if (url === '/api/accounts') return new Response(JSON.stringify(accounts), { status: 200 })
    if (url.includes('/activity-summary')) return new Response(JSON.stringify(report), { status: 200 })
    throw new Error(`Unexpected request: ${url}`)
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('activity dashboard', () => {
  beforeEach(() => history.replaceState(null, '', '/'))
  afterEach(() => vi.unstubAllGlobals())

  it('restores account and week from the URL and requests that report', async () => {
    history.replaceState(null, '', '/?account=20&week=2026-07-13')
    const fetchMock = mockApi(summary({ account: accounts[1], reportingWeek: { start: '2026-07-13', end: '2026-07-19' }, summary: { total: 0, baselineAverage: null, percentageChange: null, byEventType: { call_received: 0, lead_created: 0, appointment_set: 0 } }, locations: [] }))
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'No activity recorded yet' })).toBeInTheDocument()
    expect(screen.getByLabelText('Account')).toHaveValue('20')
    expect(screen.getByLabelText('Reporting week')).toHaveValue('2026-07-13')
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts/20/activity-summary?weekStart=2026-07-13', expect.anything())
  })

  it('updates filters in the URL and requests the selected account and week', async () => {
    const fetchMock = mockApi()
    render(<App />)
    await screen.findByText('53')
    fireEvent.change(screen.getByLabelText('Account'), { target: { value: '20' } })
    fireEvent.change(screen.getByLabelText('Reporting week'), { target: { value: '2026-07-13' } })

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/accounts/20/activity-summary?weekStart=2026-07-13', expect.anything()))
    expect(new URLSearchParams(location.search).get('account')).toBe('20')
    expect(new URLSearchParams(location.search).get('week')).toBe('2026-07-13')
  })

  it('renders totals and prior weeks from the API response contract', async () => {
    mockApi()
    render(<App />)

    expect(await screen.findByText('53')).toBeInTheDocument()
    expect(screen.getByText('vs. four-week average')).toBeInTheDocument()
    expect(screen.getByText('48.5')).toBeInTheDocument()
    const history = screen.getByRole('heading', { name: 'Four weeks of activity' }).closest('.section-block') as HTMLElement
    for (const total of ['50', '43', '62', '39']) expect(within(history).getByText(total)).toBeInTheDocument()
    expect(screen.getByText('Calls received')).toBeInTheDocument()
    expect(screen.getByText('Downtown')).toBeInTheDocument()
  })

  it('shows the explicit empty state for an account with zero activity', async () => {
    const quiet = summary({ account: accounts[1], summary: { total: 0, baselineAverage: 0, percentageChange: null, byEventType: { call_received: 0, lead_created: 0, appointment_set: 0 } }, locations: [] })
    mockApi(quiet)
    history.replaceState(null, '', '/?account=20&week=2026-07-20')
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'No activity recorded yet' })).toBeInTheDocument()
    expect(screen.getByText(/within observed dataset coverage/)).toBeInTheDocument()
    expect(screen.queryByText(/%/)).not.toBeInTheDocument()
  })

  it('suppresses percentages when baseline coverage or baseline values are missing', async () => {
    const incomplete = summary({
      coverage: { status: 'incomplete', baselineWeeksRequired: 4, baselineWeeksAvailable: 2 },
      summary: { total: 53, baselineAverage: null, percentageChange: null, byEventType: { call_received: 33, lead_created: 13, appointment_set: 7 } },
      baselineWeeks: [{ start: '2026-07-13', end: '2026-07-19', total: 50, byEventType: null }, { start: '2026-07-06', end: '2026-07-12', total: null, byEventType: null }, { start: '2026-06-29', end: '2026-07-05', total: 62, byEventType: null }, { start: '2026-06-22', end: '2026-06-28', total: null, byEventType: null }],
      locations: [{ name: 'Downtown', total: 9, byEventType: { call_received: 5, lead_created: 3, appointment_set: 1 }, baseline: { weeklyTotals: [8, null, 7, null] } }],
    })
    mockApi(incomplete)
    render(<App />)

    expect(await screen.findByText(/2 of 4 prior weeks covered/)).toBeInTheDocument()
    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
    expect(screen.queryByText(/\+9\.3%/)).not.toBeInTheDocument()
    expect(screen.getByText('History unavailable')).toBeInTheDocument()
  })

  it('shows a recoverable error when the report request fails', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => String(input) === '/api/accounts'
      ? new Response(JSON.stringify(accounts), { status: 200 })
      : new Response(JSON.stringify({ detail: 'Invalid reporting week.' }), { status: 400 }))
    vi.stubGlobal('fetch', fetchMock)
    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid reporting week.')
    expect(screen.getByRole('button', { name: /Try again/ })).toBeInTheDocument()
  })
})
