# Prefab authoring

Topaz gameplay objects own behavior, collision and static configuration. Replaceable visual children isolate imported art. Persistent instance IDs belong to generated or placed World objects; reusable prefabs do not own saved instance IDs.

Keep authored definitions and prefabs under the subsystem that uses them. Runtime terrain bindings and neutral prototype character art live under Presentation/Rendering/Environment. Reusable combat projectiles live under Gameplay/Combat/Prefabs. Licensed raw assets remain under ThirdParty with their source and license records.

Use Prefab Mode or AssetDatabase APIs, preserve GUIDs when moving assets, and update serialized visual bindings when replacing geometry. CharacterVisual exposes body, hands and attachment sockets; gameplay timing must not depend on placeholder animation. Run asset-reference checks and relevant gameplay tests after replacements. Retired migration/reconstruction commands are not part of normal authoring.
