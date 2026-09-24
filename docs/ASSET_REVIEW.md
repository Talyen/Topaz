# Reviewing imported art

The owner records choices in [`AssetReview/decisions.json`](../AssetReview/decisions.json),
keyed by Unity asset GUID so renames do not discard them. **Unmarked** assets
remain available; `keep` is optional explicit approval. `maybe` holds an asset
for later review, while `archive` keeps it out of new environments. Agents do
not assign review decisions on the owner's behalf.

## Open the gallery on this Mac

From the repository root:

```sh
python3 tools/asset_review/server.py
```

Open <http://127.0.0.1:8765/>. The server listens only on this Mac and saves
each choice immediately. The gallery offers pack, type, decision, and name
filters. Click an image to enlarge it. Click a selected decision again to
clear it. `1`, `2`, and `3` choose Keep, Maybe, and Archive when a card is
focused. "Decide on this page" applies to the 24 visible cards, and the export
link downloads a backup of the choices. Clearing an archived choice or marking
it Keep moves its FBX and `.meta` pair back into Unity's `Assets` folder.

The gallery includes models moved to [`ArtArchive/KayKit/`](../ArtArchive/KayKit/).
Their previews are retained in [`ArtArchive/Previews/`](../ArtArchive/Previews/)
so the rejected set remains visible on another checkout. Other previews under
`AssetReview/Previews/` are ignored by Git and can be regenerated with Unity:

```sh
unity run . -- -executeMethod Topaz.AssetReview.Editor.AssetReviewThumbnailExporter.GenerateAll
unity run . -- -executeMethod Topaz.AssetReview.Editor.AssetReviewThumbnailExporter.GenerateAnimations
```

The exporter uses a fixed orthographic 50°/-30° camera and each pack's atlas.
For an animation FBX, it samples eight frames from one representative clip and
shows the clip name. Check any animation bundle in the Mac game build before
using its clips in gameplay. Small props may also need an in-scene check at
Topaz's normal zoom.

## Archive workflow

On 2026-09-24, the owner's 69 Archive choices were moved outside `Assets/` with
their `.meta` files. The current scene replacements are recorded in
[`AssetReview/replacements.json`](../AssetReview/replacements.json). Source
licenses and provenance notes remain with each pack under `Assets/ThirdParty`
and have copies beside the archived files. The original extracted downloads in
the ignored `LocalSourceArchives/` are retained but are not the only archive.

For later choices, replace any scene or setup-script uses first. Then run
`python3 scripts/archive-reviewed-assets.py` to audit dependencies and
`python3 scripts/archive-reviewed-assets.py --apply` to move the selected FBX
and `.meta` pairs. The script refuses to move a source that is still referenced.
`scripts/check-asset-review.py`, which runs in `scripts/verify.sh`, rejects
remaining serialized references, C# source paths, and archived FBX files left
inside `Assets/`.
