import React from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'

function App() {
  return <main><p className="eyebrow">RELAY · ACTIVITY INSIGHTS</p><h1>DASH-247</h1><p>Activity dashboard scaffolding is ready.</p><p className="muted">Reporting summaries and account metrics are not implemented yet.</p></main>
}

createRoot(document.getElementById('root')!).render(<React.StrictMode><App /></React.StrictMode>)
