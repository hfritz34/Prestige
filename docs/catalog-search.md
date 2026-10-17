# Stored catalog search

Catalog mode exposes authenticated `GET /api/catalog/search?query=...&itemType=all&take=20`. It searches shared metadata already stored in PostgreSQL. No Spotify credentials, listening history, personal collection membership, or provider requests are required. An authenticated first request can provision the core account through the existing account transformation; the search itself does not save music or create activity.

- `query`: trimmed, 1–120 characters. Case-insensitive substring matching; `%`, `_`, and backslashes are literal characters, not wildcard patterns.
- `itemType`: `track`, `album`, `artist`, or `all` (default), case-insensitive.
- `take`: 1–50 results **per requested type**, default 20. Prefix matches in the item's own name come first, then name and ID for deterministic ties.
- Tracks match track names, album names, or credited artist names. Albums match album or credited artist names. Artists match their own names.
- Responses always contain `tracks`, `albums`, and `artists` arrays, using the existing card metadata contracts. Unrequested types and no matches return empty arrays. Missing/invalid queries or limits return 400; anonymous requests return 401.
- Legacy and Local Demo reject this route with 409. Their web clients retain the provider-specific search path.

Result limits apply before loading related metadata. This first implementation uses substring queries, not a full-text index, so it bounds returned rows rather than guaranteeing bounded database scan cost. Review query plans and add indexed search as the catalog grows. It does not expose a catalog export or calculate global popularity from listening activity.

The web home and favorite-setup searches now use this route in Catalog mode. They debounce input, reuse results briefly, show empty/error states, and allow retry. Home search opens the existing song/album/artist detail routes with card metadata. Viewing a result does not save collection membership; favoriting, pinning, or rating explicitly saves it. Catalog detail reads also skip the old automatic Spotify album-track enrichment. Those old detail contracts still need migration to collection flags and stored album order.

This does **not** populate an empty catalog or fetch missing releases. Provider selection, metadata ingestion/provenance, internal IDs versus provider IDs, release identity, and stored disc/track order remain outstanding. Existing DTOs/IDs are retained for compatibility during this step. An empty deployment will return no matches until catalog population is implemented.

Validation includes real PostgreSQL tests for case-insensitive matching, literal special characters, deterministic limits and nested artwork, plus HTTP tests for authentication, request limits and finding/saving music with no provider credentials. A detail-service regression test fails if catalog browsing touches the absent Spotify dependency. Fixture-backed browser checks cover all three search types, special-character queries, failed-search recovery, no matches, viewing unsaved music before pinning, and the Legacy compatibility path. These are local checks, not validation of a deployed provider integration.
