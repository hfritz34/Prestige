# Catalog ratings on the web

In Catalog mode, `/rating` now starts from saved music or stored-catalog search. It does not read top-listening or recently-played lists. A user can rate known music before saving it explicitly or importing any listening history. The existing atomic rating write adds collection membership and preserves favorite/pin flags. Legacy and Local Demo retain their previous rating screen.

## Read contract

Authenticated `GET /api/catalog/ratings/{itemType}?skip=0&take=50` returns the current account's ratings with matching nested `track`, `album`, or `artist` metadata. `itemType` is track/album/artist; `skip` is nonnegative and `take` is 1–100. Results are ordered by category display order, category position, then item ID. Metadata is loaded in batches for the bounded page. This route requires Catalog mode and rejects Legacy/Local Demo with 409.

Each entry contains a `rating` object using the existing rating response fields plus the matching metadata object. Song `personalScore` is null: display `rankWithinAlbum`, never the old category placeholder score. Album/artist scores remain numeric. A missing catalog reference is represented by absent metadata rather than silently dropping the rating. Removing collection membership does not remove the rating from this list. Ownership comes from the server-resolved account; there is no caller-supplied owner parameter.

## Rating flow

The dialog reads categories from the API and refreshes the existing ratings before comparisons. Comparisons exclude the item being re-rated and use category IDs, not inferred score bands. Songs compare only against the same album and category. Albums and artists compare within their respective category. The dialog uses binary insertion choices and writes the final chosen position through the existing `/api/rating/save` route. It does not make intermediate `/compare` mutations, so cancelling comparisons leaves persisted rankings unchanged.

Song insertion positions are one-based within an album category; album/artist insertion indexes are zero-based. Album-wide song ranks still span categories using the existing category order. The dialog blocks duplicate submissions while saving, remains open on failure, and supports retry. A failed read never silently becomes a first-place rating. Removing a rating keeps the saved music. After writes, rating lists, collection/favorite/pin cards, and album/artist context queries are invalidated.

Catalog album/artist re-rating now excludes the target's own old score, uses the existing score-bound/midpoint/top-redistribution formulas, and assigns contiguous positions to the chosen order. Moving categories also repairs positions in the old category. These changes share the existing account transaction/lock. Legacy rating calculations are unchanged. The old independent comparison endpoint remains available; the new web dialog does not write comparison-history events.

## Validation and remaining work

HTTP tests cover authentication, owner isolation, pagination, null song scores, metadata, and ratings surviving collection removal. PostgreSQL tests cover album/artist re-rating order, category moves, and all three enriched metadata types. Fixture-backed browser checks cover an empty saved collection, search-to-rating, same-album comparisons, self-exclusion, cancellation, read/save/delete failures, retries, category selection, rank versus score display, and more than 100 ratings.

This is the functional rating integration, not the separate visual redesign. Catalog population, stored album order, shareable catalog/profile routes, social visibility, live deployment checks, and broader scoring/concurrency characterization remain outstanding. An empty catalog still has no music to search. Comparison choices are based on the ratings read when the dialog starts; no revision/conflict prompt has been introduced for simultaneous edits in multiple browser windows.
