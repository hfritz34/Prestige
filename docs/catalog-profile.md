# Catalog owner profile

In Catalog mode, `/profile` reads the server-resolved account from `AccountProvider` and offers three views:

- **Showcase:** the current account's favorites or pins from `/api/collection/showcase`. This is the same selection friends see; a favorite that is also pinned appears once.
- **Ratings:** authoritative track/album/artist opinions from `/api/catalog/ratings/{itemType}`, including rated music no longer saved in the collection.
- **Saved music:** the owner's collection from `/api/collection`. Ordinary saved items remain private; users can independently favorite or pin an item here to display it to friends.

All lists follow the existing bounded API pagination until exhausted. Showcase queries filter on the server before pagination, so the profile does not need to download ordinary saved items to show its curated selection. No new cloud service, background process, or database migration is required.

## Curation and failures

Favorite and pin buttons send a partial PATCH for only the selected flag. Controls wait for the write and refresh, and failures keep the last loaded choices visible with an error. Clearing both flags removes an item from the showcase without deleting its collection membership or rating. A successful change invalidates owner profile lists alongside existing favorite/pin caches. Rating changes already invalidate the collection and rating query families.

Read errors are shown separately from empty states, with retry controls. An empty profile links to rating/search and friends, and explains how to populate the showcase. The owner account's nickname, picture, and optional bio are displayed; email is not rendered on the profile. Profile settings and other legacy web sections still need their own Catalog review.

## Album track rankings

Owner and friend profiles use shared music cards and ranking rendering. Tracks are grouped by album ID, not title, and sorted by `rankWithinAlbum` across rating categories. Two releases with the same title remain distinct groups. The UI states that these are rated tracks, not a complete album tracklist. Album/artist scores remain numeric; tracks show category and album rank. Missing metadata remains visible rather than silently dropping the opinion.

Friend pages retain their accepted-friendship checks and read-only behavior. Catalog owner pages do not mount listening-based Top, Recently Played, or playback components. Legacy/demo mode retains the previous profile screen.

## Validation

November 10, 2026: 45 API tests passed with PostgreSQL 15 enabled, zero skips. The owner showcase HTTP test covers union filtering without duplicates, pagination, owner isolation, parity with accepted-friend reads, anonymous rejection, invalid pagination, and preservation of saved music/ratings when flags are cleared.

The Catalog production web build passed. Chrome checks with fixture API responses covered empty/saved/showcase/ratings views, 101-entry pagination, partial flag updates, failed writes and retries, reload persistence, failed reads/category reads and recovery, release grouping, album rank order, missing metadata, mobile width, and owner/friend isolation. No legacy/provider/history calls or browser exceptions occurred in those flows. These checks do not verify live Auth0 or a deployed API. Production deployment and catalog population remain outstanding.
