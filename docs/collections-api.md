# Personal collection API

Catalog deployments now store curation in `CollectionEntry`, separate from the legacy `UserTrack`, `UserAlbum`, and `UserArtist` listening aggregates. Apply the `PersonalCollections` PostgreSQL migration before using these endpoints. They are disabled in Legacy and Local Demo modes; existing clients retain their old contracts until migrated.

All routes require authentication and infer the owner from the server-resolved account. They accept no owner ID. They do not call Spotify, fetch metadata, or create listening totals. Music must already exist in the catalog; unknown items return 404.

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/collection` | List the current user's collection, most recently updated first. Optional `itemType`, `favoritesOnly`, `pinnedOnly`, `skip` (default 0) and `take` (default 50, maximum 100). |
| GET | `/api/collection/{itemType}/{itemId}` | Read one saved item, or 404 if it is not saved. |
| PUT | `/api/collection/{itemType}/{itemId}` | Save known music and set its desired favorite/pin flags. |
| PATCH | `/api/collection/{itemType}/{itemId}` | Save known music and change only supplied flags; omitted flags are preserved. |
| DELETE | `/api/collection/{itemType}/{itemId}` | Remove collection membership, favorite and pin state; keep the separate rating and listening history. Repeating deletion returns 204. |

`itemType` is `track`, `album`, or `artist`, case-insensitive. PUT accepts a complete desired state:

```json
{ "isFavorite": true, "isPinned": false }
```

For PUT, omitted flags default to false. For PATCH, omitted or null flags preserve existing values; an empty object saves membership without altering flags. Web clients use PATCH to avoid overwriting a favorite when changing a pin, or vice versa. Repeated identical PUTs preserve timestamps and do not duplicate membership. Flags are independent: pinning need not imply favoriting. Database constraints enforce one matching catalog reference, a valid owner and unique membership. PostgreSQL writes take a transaction-scoped lock on the account row so simultaneous curation changes for one account serialize; different accounts can write independently.

Responses include the matching nested `track`, `album`, or `artist` metadata for cards, catalog names/artwork, flags, saved/updated timestamps and the current rating, if present. Ratings are read from `Rating` rather than copied into the collection. Track ratings expose category, category position and album rank, with `score: null`: the old track score is a category placeholder, not a global song score. Collection responses have no listening-time or prestige-tier fields.

In catalog mode, saving a rating through `/api/rating/save` also saves the item to the collection, preserving existing favorite/pin flags. Membership, rating writes and rank changes commit together. Rating deletion keeps collection membership. Comparisons, rating saves/deletes and collection writes share the same account lock on PostgreSQL. Invalid types, positions and comparison winners return 400; unknown catalog items/categories return 404. Track comparisons are limited to one album. Track positions remain one-based within a category (zero uses the existing comparison-based placement); album/artist insertion positions retain their existing zero-based behavior.

Regression tests cover concurrent writes, failure after an intermediate save, category moves, deletion, owner isolation and collection/rating independence. The existing category scoring and comparison algorithms remain in place; further algorithm and social-authorization review is still required before public release.

The web now uses this contract for favorites, pinned lists and detail-page pin state in Catalog mode. It reads every bounded page, invalidates shared card queries after updates, displays write errors, and treats a failed pin read as unavailable rather than unpinned. Collection cards explicitly have no listening history, so they do not display fabricated minutes or prestige progress. Pinned cards display the saved rating or album rank when available. Opening catalog screens does not start Spotify currently-playing polling.

Set `VITE_PRESTIGE_MODE=Catalog` (the normal web default) to match the migrated API. Use `Legacy` only against a legacy deployment. Local Demo defaults to Legacy unless explicitly overridden for fixture-based integration tests; its collection API remains disabled.

The native client still uses legacy favorites/pin routes. Web home/favorite searches now use [stored catalog search](catalog-search.md). Listening-based home/profile sections, album/artist context, and friend comparisons still need further catalog work. Catalog population and provider identity separation remain outstanding; an empty catalog has no music to find.

Validation: API tests cover independent/concurrent PATCH updates and nested card metadata. Browser checks with fixture API responses cover favorite/pin independence, state after reload, read/write failure recovery, album/artist navigation, pagination beyond 100 favorites, setup failures, and absence of playback polling. These checks are not evidence of a live Auth0/Spotify production deployment.
