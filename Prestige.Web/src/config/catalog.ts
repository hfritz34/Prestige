// Local Demo retains its seeded legacy data unless explicitly overridden for integration tests.
export const usesCatalogCollections = (import.meta.env.VITE_PRESTIGE_MODE ??
  (import.meta.env.VITE_LOCAL_DEMO === 'true' ? 'Legacy' : 'Catalog')) === 'Catalog';
