/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_API_ADDRESS: string
    readonly VITE_API_AUDIENCE: string
    readonly VITE_WEB_ADDRESS: string
}
  
interface ImportMeta {
    readonly env: ImportMetaEnv
}