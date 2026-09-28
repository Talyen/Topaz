# Audio Asset Policy

Topaz uses Unity's built-in audio importer. Keep source masters in the local Raw Asset Library and place only active working assets or generated runtime clips under `Assets`. Unity transcodes imported audio for the selected build target, so choose settings by sound use rather than applying one codec to every clip. See Unity's [Audio Clip Import Settings](https://docs.unity3d.com/6000.6/Documentation/Manual/class-AudioClip.html) and [audio compression guide](https://docs.unity3d.com/6000.6/Documentation/Manual/AudioFiles-compression.html).

## Storage and provenance

- Store Sonniss ZIPs, uncompressed source WAVs, licenses, manifests, and hashes under `~/Documents/Raw Asset Library/Sounds/SonnissGDC/`. Keep unused audition candidates under `Staged/`; store source files needed by active editor tools under `ActiveSources/`.
- Keep raw media out of the public Topaz repository. Retain each original file's SHA-256, license, original path, and any conversion profile in the external source manifest or adjacent `SOURCE.md`.
- Keep active Unity assets with their `.meta` files. Move original WAVs and their source metadata out only after creating/importing a working asset and updating all scene, prefab, data-asset, and code references through Unity's AssetDatabase.
- Editor-only tools may stage a temporary copy under `Assets/ThirdParty/Sonniss/EditorTemp` to use Unity's decoder. Remove those imports in a `finally` path; gameplay must not load media from the external library.

## Unity import defaults

| Sound use | Working asset | Unity import profile |
| --- | --- | --- |
| Long ambience or music | Lossless WAV working copy; retain stereo for wide 2D beds | Streaming, Vorbis, quality 0.65, preserve the working sample rate, Load In Background on, Preload Audio Data off |
| Short one-shot or repeated effect | Keep tiny Ogg sources as-is; use PCM or ADPCM for new WAV effects when appropriate | Decompress On Load for small clips; use ADPCM for noisy effects played frequently when its artifacts are acceptable |
| Derived short foley | Small mono WAV outputs generated from external source masters | Keep the current compact Decompress On Load profile unless profiling shows a meaningful cost |

For Topaz's current Sonniss ambience, retain the original 96 kHz / 24-bit WAV masters externally and use 48 kHz / 24-bit stereo PCM working copies in `Assets/ThirdParty/Sonniss/Runtime/`. Unity then streams them as Vorbis at quality 0.65. Do not apply a fixed 22.05 kHz override to Vorbis clips: Unity's sample-rate reduction controls are documented for PCM and ADPCM. Use stereo for the current non-spatial ambience beds.

The Kenney and JaggedStone Ogg effects were retired and removed from `Assets/`; their source files and records remain in the local archive, and the associated weapon/spell cues are unwired pending Sonniss selections. For any future selected short Ogg effects, keep the compressed Ogg source as the runtime representation rather than encoding it through another lossy format. Keep the eight generated Storybook footstep clips as runtime assets and keep their original Grass/Gravel inputs in `ActiveSources/`.

Unity recommends Streaming for continuous audio and high-quality compression for long files. It also describes ADPCM as a space/CPU option for noisy repeated effects, while warning that artifacts are more noticeable on smooth ambience. Apply these choices by category; no one needs to audition every unused candidate clip. Retain quality 0.65 for the current ambience rather than lowering it without measured size benefit and representative playback review. Audiokinetic's codec guidance similarly treats CPU, memory, storage, and quality as a tradeoff and describes Vorbis and Opus as different cost/quality choices; Topaz does not use Wwise or FMOD.

## Validation

- Verify source hashes and license records before moving or transforming any original.
- After import or replacement, read back importer settings in Unity, confirm affected serialized audio references resolve, and confirm editor-generated temporary files are gone.
- For ordinary sound/tuning changes, audition the affected interaction in the Editor and the completed player batch. Run focused tests when routing/behavior changes; build once when import/platform behavior needs validation. Check build size or the Audio Profiler for a specific codec/memory/CPU question, not every sound edit. Follow the [proportional workflow](AGENT_WORKFLOW.md#iterate-and-hand-off).
