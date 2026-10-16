/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_AUTH0_DOMAIN: string
    readonly VITE_AUTH0_CLIENT_ID: string
    readonly VITE_API_ADDRESS: string
    readonly VITE_API_AUDIENCE: string
    readonly VITE_WEB_ADDRESS: string
    readonly VITE_LOCAL_DEMO?: string
    readonly VITE_PRESTIGE_MODE?: "Catalog" | "Legacy"
}
  
interface ImportMeta {
    readonly env: ImportMetaEnv
}
