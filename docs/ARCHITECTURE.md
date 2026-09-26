# Architecture

[BASELINE.md](BASELINE.md) is the current product contract.

## Ownership

Use GameObjects/components, ScriptableObject definitions, and plain serializable runtime records. WorldSession coordinates the active Character–World pair; region travel/recovery lives in its Regions partial. It owns atomic inventory/world transactions and persistence. Do not introduce parallel save authorities.

WoodlandPlan produces deterministic data with separate layout/resource/decoration streams. WoodlandRegion realizes terrain, art, resources, reserved routes, and runtime navigation. RegionBuildings binds only the active region's structures, resolves region-local coordinates, validates placement, and rebuilds navigation after changes.

Campfire objects provide discovery and arrival points. Fixed destinations are catalogued from build scenes; player camps resolve from saved World structures even while their region is unloaded. CampSafety owns active protection and navigation exclusion; combat damage and projectiles consult it. Protection does not award kills or discard respawn deadlines.

Static definitions remain separate from mutable state. Characters own inventory/equipment/progression. Worlds own terrain identity, structures/storage, resource state, pickups, and encounter deadlines. Visits own position, discoveries, and recovery selection.

## Saves

Baseline-v1 is a fresh versioned save root. Historical migrations are retired. ProfileRepository retains validated snapshots, atomic replacement, backup recovery, and coalesced background writes. Unsupported/corrupt saves are reported rather than silently overwritten. Generator settings and version are persisted. No old saves require compatibility.

## Presentation

Unity 6000.6.2f1, URP 17.6, Input System 1.20, Cinemachine 6.6, AI Navigation 2.0.14, uGUI/TMP, Shader Graph and VFX Graph. First-party systems own rendering, camera collision, navigation, input and UI; small Topaz components implement game rules. Third-party art is referenced through replaceable owned visuals.
