# Personal collection API

Catalog deployments now store curation in `CollectionEntry`, separate from the legacy `UserTrack`, `UserAlbum`, and `UserArtist` listening aggregates. Apply the `PersonalCollections` PostgreSQL migration before using these endpoints. They are disabled in Legacy and Local Demo modes; existing clients retain their old contracts until migrated.

All routes require authentication and infer the owner from the server-resolved account. They accept no owner ID. They do not call Spotify, fetch metadata, or create listening totals. Music must already exist in the catalog; unknown items return 404.

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/collection` | List the current user's collection, most recently updated first. Optional `itemType`, `favoritesOnly`, `pinnedOnly`, `skip` (default 0) and `take` (default 50, maximum 100). |
| GET | `/api/collection/{itemType}/{itemId}` | Read one saved item, or 404 if it is not saved. |
| PUT | `/api/collection/{itemType}/{itemId}` | Save known music and set its desired favorite/pin flags. |
| DELETE | `/api/collection/{itemType}/{itemId}` | Remove collection membership, favorite and pin state; keep the separate rating and listening history. Repeating deletion returns 204. |

`itemType` is `track`, `album`, or `artist`, case-insensitive. PUT accepts a complete desired state:

```json
{ "isFavorite": true, "isPinned": false }
```

Omitted flags default to false. Repeated identical PUTs preserve timestamps and do not duplicate membership. Flags are independent: pinning need not imply favoriting. Database constraints enforce one matching catalog reference, a valid owner and unique membership. PostgreSQL writes take a transaction-scoped lock on the account row so simultaneous curation changes for one account serialize; different accounts can write independently.

Responses contain catalog names/artwork, flags, saved/updated timestamps and the current rating, if present. Ratings are read from `Rating` rather than copied into the collection. Track ratings expose category, category position and album rank, with `score: null`: the old track score is a category placeholder, not a global song score. Collection responses have no listening-time or prestige-tier fields.

This is the API foundation. The existing web/native favorites and pin screens still use legacy routes and storage; moving those consumers to this contract is a separate integration step. Catalog discovery and provider identity separation are also still outstanding.
