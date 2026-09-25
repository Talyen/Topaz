# Home to Graveyard playable slice

Status: implemented for Mac owner review. Home is the safe base. Walking south through its trail loads the adjacent Graveyard; the exterior grave path and crypt entrance are at the far side of that area. Leaving the crypt returns outside that entrance. The first playable pass needs a 60 Hz Mac review of the trail seam, graveyard composition, crypt return, and encounter pacing.

## Player route

The home Campfire, crafting, storage, trees, and rocks remain in Bootstrap. The home practice enemy and exterior crypt grave path are gone. Walk through the south edge of the home trail to cross the Graveyard boundary; there is no interaction prompt or half-hour clock jump. A dark covered load keeps the player and camera stable and resumes just inside the matching Graveyard edge. Walking back through the Graveyard's outer trail reverses the transition. The Graveyard is an authored scene at `Assets/Topaz/World/Scenes/Graveyard.unity`, loaded additively while the persistent player and save owner stay in Bootstrap.

The existing forest clearing now leads through two Skeleton Scouts and a wide-sweep Guardian toward a grave path and the crypt entrance. KayKit Forest and Halloween assets provide the transition from trees to graves and dead growth. The crypt remains an authored interior. Its door can be used while outdoor enemies are alive; the Graveyard cache can likewise be opened without defeating the Guardian. The Graveyard Campfire still provides a recovery point for the current Character–World Visit.

## Repeatable encounters and rewards

Every authored Skeleton has a stable spawn ID. A defeat saves a World-owned return deadline 24 in-game hours later. Rest and defeat recovery advance the same clock; menus, loading, and a closed game do not. Area re-entry or recovery cannot revive an enemy early. A due enemy returns when its spawn is away from the player and out of view, or after entering its unloaded area. The home practice foe is removed.

Each Skeleton defeat creates one Bone Fragment pickup. Scouts and Minions have no additional drop. Warriors and the Graveyard Guardian also leave one existing Sword or Shield, chosen with equal odds at defeat. The Rogue also leaves a Skeleton Crossbow; the Mage also leaves a Crypt Staff. Pickups have unique instance IDs, stay in their World and region, and wait when the backpack is full. Bone Fragments stack and can be stored; recipes and salvaging come later. The Mage's shrine light goes dark on defeat and returns with the Mage.

The Graveyard supply cache gives three Wood and the crypt cache gives two Stone and one Iron. Both stay visibly present when empty and refill 72 World hours after a successful claim. A failed claim due to backpack capacity does not start the timer. The crypt shortcut remains a permanent World change.

## Implementation and review

Collection version 9 adds enemy return deadlines and cache refill times. Earlier claimed-cache flags had no timestamp, so older saves start the new cycle with stocked caches. Collection version 11 shifts Graveyard visits and ground pickups ten world units south with the authored scene, preserving old edge saves and loot. Legacy clearing visits and drops are transformed from the former scene origin through that same migration; the persisted region ID `expedition.clearing` remains stable while its displayed name is Graveyard. Character-owned items and existing World pickups remain intact.

Unity's [asynchronous additive scene loading](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) and the installed [AI Navigation NavMesh Surface](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html) remain the foundations. Topaz code handles the paired trail triggers, World-clock deadlines, save migration, and loot rules. No content-streaming package is needed at this scale.

In a Mac build, review whether the two trail edges read as one path, whether grave art guides the player naturally toward the crypt, whether the crypt exit appears outside its Graveyard entrance, and whether renewed enemies and caches feel legible. Check 1280×720 and the far camera zoom. Performance claims require a representative standalone capture and the designated Windows PC.
