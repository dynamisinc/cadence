/// <reference types="vite/client" />

declare const __APP_VERSION__: string
declare const __BUILD_DATE__: string
declare const __COMMIT_SHA__: string

interface ImportMetaEnv {
  readonly VITE_API_URL: string
  readonly VITE_SIGNALR_URL?: string
  readonly VITE_PWA_REGISTER_TYPE?: string
  readonly VITE_APPINSIGHTS_CONNECTION_STRING?: string
  readonly VITE_APP_VERSION?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
