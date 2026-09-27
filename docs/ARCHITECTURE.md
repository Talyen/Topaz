# Architecture

[BASELINE.md](BASELINE.md) is the current product contract.

## Ownership

Use GameObjects/components, ScriptableObject definitions, and plain serializable runtime records. WorldSession coordinates the active Character–World pair; camp travel/recovery lives in its Regions partial. It owns atomic inventory/world transactions and persistence. Do not introduce parallel save authorities.

WildernessPlan samples deterministic world-space terrain and independent resource/decoration streams. WoodlandRegion is the scene entry point for StreamedWilderness, which realizes bounded Terrain chunks, art, resource instances and runtime navigation. RegionBuildings retains World-owned structure records while loading nearby visuals. WorldStorageIndex queries saved chests within 30 m. Navigation changes are queued asynchronously.

Campfire objects provide discovery and arrival points. Fixed destinations are catalogued from build scenes; player camps resolve from saved World structures even while their chunk is unloaded. CampSafety owns active protection and navigation exclusion; combat damage and projectiles consult it. Protection does not award kills or discard respawn deadlines.

Static definitions remain separate from mutable state. Characters own inventory/equipment/progression. Worlds own terrain identity, structures/storage, resource state, pickups, and encounter deadlines. Visits own position, discoveries, and recovery selection.

## Saves

Storybook-v2 is a fresh versioned save root. Historical migrations are retired. ProfileRepository retains validated snapshots, atomic replacement, backup recovery, and coalesced background writes. Unsupported/corrupt saves are reported rather than silently overwritten. Generator settings and version are persisted. No old saves require compatibility.

## Presentation

Unity 6000.6.2f1, URP 17.6, Input System 1.20, Cinemachine 6.6, AI Navigation 2.0.14, uGUI/TMP, Shader Graph and VFX Graph. First-party systems own rendering, camera collision, navigation, input and UI; small Topaz components implement game rules. Third-party art is referenced through replaceable owned visuals.
