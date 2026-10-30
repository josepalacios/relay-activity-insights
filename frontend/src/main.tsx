import { useCallback, useEffect, useMemo, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'

type Account = { id: number; name: string; timeZone: string }
type Counts = Record<'call_received' | 'lead_created' | 'appointment_set', number>
type Week = { start: string; end: string; total: number | null; byEventType: Counts | null }
type LocationSummary = { name: string; total: number; byEventType: Counts; baseline: { weeklyTotals: (number | null)[] } }
type Summary = {
  account: Account
  reportingWeek: { start: string; end: string }
  baselineWeeks: Week[]
  coverage: { status: string; baselineWeeksRequired: number; baselineWeeksAvailable: number }
  summary: { total: number; baselineAverage: number | null; percentageChange: number | null; byEventType: Counts }
  locations: LocationSummary[]
}

const DEFAULT_WEEK = '2026-07-20'
const TYPES: { key: keyof Counts; label: string; short: string }[] = [
  { key: 'call_received', label: 'Calls received', short: 'Calls' },
  { key: 'lead_created', label: 'Leads created', short: 'Leads' },
  { key: 'appointment_set', label: 'Appointments set', short: 'Appointments' },
]
const formatNumber = new Intl.NumberFormat()
const dateLabel = (iso: string, options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' }) =>
  new Date(`${iso}T12:00:00`).toLocaleDateString('en-US', options)
const rangeLabel = (start: string, end: string) => `${dateLabel(start)} – ${dateLabel(end)}, ${end.slice(0, 4)}`
const weekOptions = (): string[] => {
  const values: string[] = []
  const end = new Date(`${DEFAULT_WEEK}T12:00:00Z`)
  for (let i = 0; i < 25; i++) {
    const d = new Date(end)
    d.setUTCDate(d.getUTCDate() - i * 7)
    values.push(d.toISOString().slice(0, 10))
  }
  return values
}
const apiError = async (response: Response) => {
  try {
    const body = await response.json()
    return body.detail || body.error || body.title || `Request failed (${response.status})`
  } catch { return `Request failed (${response.status})` }
}

export function App() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [accountId, setAccountId] = useState(new URLSearchParams(location.search).get('account') || '')
  const [week, setWeek] = useState(new URLSearchParams(location.search).get('week') || DEFAULT_WEEK)
  const [accountsLoading, setAccountsLoading] = useState(true)
  const [loading, setLoading] = useState(false)
  const [retry, setRetry] = useState(0)
  const [error, setError] = useState('')
  const [data, setData] = useState<Summary | null>(null)

  const syncFromUrl = useCallback(() => {
    const params = new URLSearchParams(location.search)
    setAccountId(params.get('account') || '')
    setWeek(params.get('week') || DEFAULT_WEEK)
  }, [])

  useEffect(() => {
    addEventListener('popstate', syncFromUrl)
    return () => removeEventListener('popstate', syncFromUrl)
  }, [syncFromUrl])

  useEffect(() => {
    let alive = true
    fetch('/api/accounts').then(async response => {
      if (!response.ok) throw new Error(await apiError(response))
      return response.json() as Promise<Account[]>
    }).then(list => {
      if (!alive) return
      setAccounts(list)
      setAccountsLoading(false)
      if (!accountId && list.length) {
        const next = new URL(location.href)
        next.searchParams.set('account', String(list[0].id))
        next.searchParams.set('week', week || DEFAULT_WEEK)
        history.replaceState(null, '', next)
        setAccountId(String(list[0].id))
      }
    }).catch(e => { if (alive) { setError(e.message); setAccountsLoading(false) } })
    return () => { alive = false }
  }, [])

  useEffect(() => {
    if (!accountId || accountsLoading) return
    const controller = new AbortController()
    setLoading(true); setError(''); setData(null)
    fetch(`/api/accounts/${encodeURIComponent(accountId)}/activity-summary?weekStart=${encodeURIComponent(week)}`, { signal: controller.signal })
      .then(async response => { if (!response.ok) throw new Error(await apiError(response)); return response.json() as Promise<Summary> })
      .then(result => { setData(result); setLoading(false) })
      .catch(e => { if (e.name !== 'AbortError') { setError(e.message); setLoading(false) } })
    return () => controller.abort()
  }, [accountId, week, accountsLoading, retry])

  const updateFilter = (nextAccount: string, nextWeek: string) => {
    const url = new URL(location.href)
    if (nextAccount) url.searchParams.set('account', nextAccount); else url.searchParams.delete('account')
    url.searchParams.set('week', nextWeek)
    history.pushState(null, '', url)
    setAccountId(nextAccount); setWeek(nextWeek)
  }

  const locations = useMemo(() => {
    if (!data) return []
    return [...data.locations].sort((a, b) => {
      const avgA = a.baseline.weeklyTotals.length === 4 && a.baseline.weeklyTotals.every(x => x !== null) ? a.baseline.weeklyTotals.reduce<number>((s, x) => s + (x ?? 0), 0) / 4 : null
      const avgB = b.baseline.weeklyTotals.length === 4 && b.baseline.weeklyTotals.every(x => x !== null) ? b.baseline.weeklyTotals.reduce<number>((s, x) => s + (x ?? 0), 0) / 4 : null
      return (avgB === null ? -1 : Math.abs(b.total - avgB)) - (avgA === null ? -1 : Math.abs(a.total - avgA))
    })
  }, [data])

  const baselineAverage = data?.summary.baselineAverage ?? null
  const change = data?.summary.percentageChange ?? null
  const emptyAccount = data?.locations.length === 0 && data.summary.total === 0

  return <main className="shell">
    <header className="topbar"><a className="brand" href="/" aria-label="Relay Activity Insights home"><span className="brand-mark">R</span><span>relay<span className="brand-light"> / insights</span></span></a><span className="demo-tag">WEEKLY ACTIVITY</span></header>
    <section className="intro"><p className="eyebrow">ACCOUNT PERFORMANCE</p><h1>Activity, with context.</h1><p className="intro-copy">See how this week compares with each account’s recent activity.</p></section>
    <section className="filters" aria-label="Dashboard filters">
      <label>Account<select aria-label="Account" value={accountId} disabled={accountsLoading || !accounts.length} onChange={e => updateFilter(e.target.value, week)}>
        {accountsLoading && <option value="">Loading accounts…</option>}{!accountsLoading && !accounts.length && <option value="">No accounts available</option>}
        {accounts.map(account => <option value={account.id} key={account.id}>{account.name}</option>)}
      </select></label>
      <label>Reporting week<select aria-label="Reporting week" value={week} onChange={e => updateFilter(accountId, e.target.value)}>{weekOptions().map(start => <option key={start} value={start}>{rangeLabel(start, new Date(new Date(`${start}T12:00:00Z`).getTime() + 6 * 86400000).toISOString().slice(0, 10))}</option>)}</select></label>
    </section>

    {accountsLoading && <div className="notice" role="status">Loading accounts…</div>}
    {!accountsLoading && !accounts.length && !error && <div className="notice"><h2>No accounts found</h2><p>The accounts API returned an empty list.</p></div>}
    {loading && <div className="notice" role="status"><span className="spinner" /> Loading weekly activity…</div>}
    {error && !loading && <div className="notice error" role="alert"><span className="notice-icon">!</span><div><h2>We couldn’t load this report</h2><p>{error}</p><button className="text-button" onClick={() => accountsLoading || !accounts.length ? location.reload() : setRetry(value => value + 1)}>Try again <span aria-hidden="true">↗</span></button></div></div>}

    {data && <>
      <div className="report-meta"><div><span className="status-dot" /> WEEK OF {dateLabel(data.reportingWeek.start, { month: 'long', day: 'numeric' }).toUpperCase()} – {dateLabel(data.reportingWeek.end, { month: 'long', day: 'numeric' }).toUpperCase()}</div><span>{data.account.timeZone}</span></div>
      {emptyAccount ? <section className="empty-state"><div className="empty-illustration" aria-hidden="true"><span>0</span><i>↗</i></div><p className="eyebrow">A QUIET START</p><h2>No activity recorded yet</h2><p>There are no observed locations or events for {data.account.name}. Activity will appear here when it’s available in the source data.</p><span className="coverage-note">Reporting week is within observed dataset coverage.</span></section> : <>
        <section className="overview-grid">
          <article className="card total-card"><div className="card-top"><span className="card-label">TOTAL ACTIVITY</span><span className="metric-icon" aria-hidden="true">↗</span></div><div className="total-value">{formatNumber.format(data.summary.total)}</div><div className="week-caption">events this week</div>
            <div className="comparison">{baselineAverage !== null ? <><span className={`change-pill ${(change ?? 0) >= 0 ? 'positive' : 'negative'}`}>{change === null ? '—' : `${change >= 0 ? '+' : ''}${change.toFixed(1)}%`}</span><span>vs. four-week average <strong>{baselineAverage.toFixed(1)}</strong></span></> : <span className="muted">{data.coverage.baselineWeeksAvailable} of 4 prior weeks covered · average unavailable</span>}</div>
          </article>
          <article className="card type-card"><div className="card-top"><span className="card-label">ACTIVITY BREAKDOWN</span><span className="subtle">THIS WEEK</span></div>{TYPES.map((type, index) => <div className="type-row" key={type.key}><span className={`type-dot type-${index}`} /><span>{type.label}</span><strong>{formatNumber.format(data.summary.byEventType[type.key])}</strong></div>)}</article>
        </section>

        <section className="section-block"><div className="section-heading"><div><p className="eyebrow">THE RECENT PICTURE</p><h2>Four weeks of activity</h2></div><span className="section-aside">{data.coverage.baselineWeeksAvailable} / 4 weeks covered</span></div>
          <div className="history-grid">{data.baselineWeeks.map((item, index) => <article className={`history-card ${item.total === null ? 'uncovered' : ''}`} key={item.start}><span className="history-index">0{index + 1}</span><span className="history-dates">{rangeLabel(item.start, item.end)}</span><strong>{item.total === null ? '—' : formatNumber.format(item.total)}</strong><span className="history-unit">{item.total === null ? 'Not covered' : 'events'}</span><div className="mini-bar" aria-hidden="true"><span style={{ width: `${item.total === null ? 0 : Math.max(5, Math.min(100, item.total / Math.max(1, ...data.baselineWeeks.map(w => w.total ?? 0), data.summary.total) * 100))}%` }} /></div></article>)}</div>
        </section>

        <section className="section-block locations-section"><div className="section-heading"><div><p className="eyebrow">WHERE IT HAPPENED</p><h2>Location activity</h2></div><p className="section-aside">Sorted by absolute difference from each location’s available baseline</p></div>
          {!locations.length ? <div className="notice compact">No locations have recorded activity in this account.</div> : <div className="table-wrap"><table><thead><tr><th scope="col">LOCATION</th><th scope="col" className="numeric">THIS WEEK</th><th scope="col">4-WEEK HISTORY</th><th scope="col" className="numeric">VS. AVG.</th></tr></thead><tbody>{locations.map(location => {
            const totals = location.baseline.weeklyTotals
            const avg = totals.length === 4 && totals.every(v => v !== null) ? totals.reduce<number>((sum, v) => sum + (v ?? 0), 0) / 4 : null
            const delta = avg !== null && avg > 0 ? (location.total - avg) / avg * 100 : null
            const maxBar = Math.max(1, ...totals.map(v => v ?? 0), location.total)
            return <tr key={location.name}><th scope="row"><span className="location-name">{location.name}</span><span className="location-types">{TYPES.map(t => location.byEventType[t.key]).join(' · ')} <span>calls · leads · appts</span></span></th><td className="numeric current-count">{formatNumber.format(location.total)}</td><td><div className="history-bars" aria-label={`Prior weekly totals: ${totals.map(v => v === null ? 'not covered' : v).join(', ')}`}>{totals.map((value, i) => <span className={value === null ? 'bar-missing' : ''} key={i} title={`${dateLabel(data.baselineWeeks[i]?.start ?? '')}: ${value ?? 'not covered'}`} style={{ height: `${value === null ? 3 : Math.max(4, value / maxBar * 24)}px` }} />)}</div><span className="bar-caption">{avg === null ? 'History unavailable' : `avg ${avg.toFixed(1)}`}</span></td><td className="numeric"><span className={`delta ${delta === null ? 'delta-muted' : delta > 0 ? 'delta-up' : delta < 0 ? 'delta-down' : ''}`}>{delta === null ? '—' : `${delta >= 0 ? '+' : ''}${delta.toFixed(1)}%`}</span></td></tr>
          })}</tbody></table></div>}
          {data.coverage.status !== 'complete' && <p className="coverage-note">Some prior weeks are outside observed dataset coverage. Historical averages are shown only when all four weeks are covered.</p>}
        </section>
      </>}
      <footer><span>Relay Activity Insights</span><span>Counts reflect events present in the source data.</span></footer>
    </>}
  </main>
}

const rootElement = document.getElementById('root')
if (rootElement) createRoot(rootElement).render(<App />)
