# Catalog friends and shared taste

Catalog social endpoints use the server-resolved account. No caller-supplied owner ID, Spotify credential, listening row, or background job is required.

## Friendship lifecycle

All routes require authentication and a migrated Catalog deployment. Legacy/demo deployments return 409 for these routes. The old `/api/Friendships/**` routes return 409 in Catalog mode so direct-add and caller-supplied owner IDs cannot bypass consent. Native clients must migrate before using Catalog social features.

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/catalog/friends?skip=0&take=50` | Accepted friends, ordered by request date then IDs. |
| GET | `/api/catalog/friends/requests?direction=incoming&skip=0&take=50` | Incoming pending requests; `outgoing` is also supported. |
| POST | `/api/catalog/friends/requests/{otherId}` | Send a request; the body does not set the acting account. |
| POST | `/api/catalog/friends/requests/{otherId}/accept` | Only the recipient can accept an incoming request. |
| POST | `/api/catalog/friends/requests/{otherId}/decline` | Only the recipient can decline an incoming request. |
| DELETE | `/api/catalog/friends/requests/{otherId}` | Cancel your pending outgoing request. |
| DELETE | `/api/catalog/friends/{otherId}` | Remove both sides of an accepted friendship. |

Lists accept `skip >= 0`, `take` 1–100. Responses describe the **other person**, including acceptance/decline: `id`, `name`, `nickname`, `profilePicUrl`, `bio`, string `status`, `requestDate`, and nullable `acceptedDate`. No tokens, email, import data, or listening history are serialized. Deletes return 204.

Self-requests and invalid pagination/direction return 400. Unknown accounts or missing incoming requests return 404. Repeated sends, acceptances, declines, cancellations, and removals are safe while the relevant state is unchanged. A declined request can be sent again. Crossed pending requests return 400 with an instruction to accept/decline the existing incoming request; they never auto-accept. No profile name has special behavior.

Accepted relationships retain the existing two-row representation, committed atomically. PostgreSQL mutations lock both account rows in ordinal ID order to serialize actions from either direction and avoid opposite-order deadlocks. No migration is needed. Concurrent sends, acceptances, removals, and crossed requests are covered against PostgreSQL; consent, isolation, validation, pagination, and legacy bypass rejection are exercised through HTTP integration tests.

This is the foundation for friends-only catalog sharing. Public profile visibility, blocking/reporting, notification delivery, and request abuse limits remain separate launch decisions/work; the legacy social endpoints are not a compatibility fallback in Catalog mode.

## Friends-only music views

`GET /api/catalog/friends/{otherId}` returns the accepted friend's public profile summary. `/showcase` returns only collection entries marked favorite or pinned, with stored music metadata. `/ratings/{itemType}` returns authoritative ratings and metadata independently of collection membership, using the same shape as `/api/catalog/ratings/{itemType}`. Both music lists accept `skip` and `take` (maximum 100).

Every read checks the current viewer's accepted friendship first. Unknown people, strangers, pending/declined requests, and removed friends receive 404; anonymous callers receive 401. There is no anonymous/public sharing in this slice. Removal revokes subsequent server reads immediately; a response already displayed in another browser is not remotely erased. No provider request or listening history is involved.

Track opinions retain album context and `rankWithinAlbum`, with null personal scores. Album/artist ratings retain derived scores and category positions. Sharing does not change, copy, or create the viewer's ratings or collection. Private account `/api/catalog/ratings` and `/api/collection` ownership remain unchanged.
