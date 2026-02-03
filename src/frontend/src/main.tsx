// Application entry point
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { checkEnvironment } from './core/utils/validateEnv'
import { initializeTelemetry } from './core/services/telemetry'

// Validate environment variables on startup
checkEnvironment()

// Initialize Application Insights telemetry (if configured)
initializeTelemetry()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
