# First authored expedition clearing

This is a small playable slice of Topaz's home-to-expedition loop. It tests a player-chosen trip, a few authored encounters, a guarded reward, a return home, and save/reload across the scene boundary. It is not a generalized region, quest, loot, or death system.

## Play the slice

1. Leave the safe home ring and approach the blue trail marker south of the homestead. Its post, light, and ground ring make the interaction point visible at the fixed camera angle. Press `E` or gamepad `X` to enter the clearing. The homestead stays loaded while the clearing loads additively.
2. Move through the clearing past two Skeleton scouts. The larger gold guardian at the far supply chest uses a slower, wider sword sweep than the practice enemy. Its red ground arc remains the authoritative attack tell. Defeat the guardian to unlock the cache.
3. Press `E` or `X` beside the chest to take **3 Wood**. The backpack must have room for all three; otherwise the cache stays unclaimed. Three Wood can craft the existing storage chest back home.
4. Return to the blue banner near the clearing entrance whenever you choose. The region unloads, while the player, inventory, home structures, and save owner stay in the Bootstrap scene. Reentering loads fresh encounters but the claimed cache remains empty.
5. Quit and reopen in the clearing or at home to check the saved region and position. A practice defeat in the clearing sends the player home through the same return path. It does not define Topaz's final death consequences.

The clearing is a separate hand-authored scene at `Assets/Topaz/World/Scenes/Expedition.unity`. Its floor and obstacles have a baked NavMesh asset; KayKit models are the same CC0 library already registered in `THIRD_PARTY_ASSETS.md`. The two scouts use the short focused attack. The guardian uses a 145-degree sweep with a longer 1.05-second warning, a sword clip, and four hit points. A successful sword strike can hit any active enemy in its aimed arc and awards Swords XP per damage dealt. Enemies stay down for the rest of that visit, then reset when the clearing is reloaded.

## State and implementation boundary

Save version **4** adds a stable region ID and a claimed-cache flag. Versions 1–3 migrate to the home region without losing their existing inventory, structures, tree state, drops, or experience. The cache is a one-time, all-or-nothing Wood grant, so a full backpack cannot silently lose the reward. Saves are not queued in the middle of a scene transition; the last completed region state remains recoverable if the game closes during a load.

`WorldSession` coordinates travel and still owns the backpack and save. `ExpeditionSceneBootstrap` owns only the clearing's authored entry, return marker, cache, NavMesh, and local enemy binding. The existing CharacterController and camera stay with the player in Bootstrap. The clearing uses Unity's maintained [asynchronous scene loading](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) and the installed [AI Navigation NavMesh Surface](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html); no new package or custom streaming system was needed.

Run `Topaz/Build Expedition Study` after rebuilding the underlying world loop. The setup adds the scene to the global Build Profile scene list. It preserves the existing Bootstrap scene content and rewires the home trail marker. The first bake is stored under `Assets/Topaz/World/Expedition/Navigation/`. If the clearing's collision geometry changes later, rebake and review that NavMesh asset before a build.

Review the Mac build for route clarity, attack readability, return pacing, and whether three Wood feels like a worthwhile first reward. A representative performance comparison begins once the slice's encounter density and effects are stable; the empty Bootstrap scene alone cannot establish the Windows 1080p/120 target.
