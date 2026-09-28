# Performance process

This process applies to a reproduced hitch, a plausible performance-sensitive change, explicit performance work or platform certification. It is not the everyday art/feel loop. Use [workflow](AGENT_WORKFLOW.md#iterate-and-hand-off) for minute-scale feedback. Preserve standalone performance requirements when making measured claims; missing certification does not block independent improvement work.

## Near-term baseline

Use the Mac player on a 60 Hz display to review input response, camera motion, and visible jitter. The current desktop setting uses one vertical sync per frame, so this Mac's 60 Hz display provides a 60 FPS presentation baseline without a software frame cap. Fix obvious hitches and retain their reproduction steps. Use repeatable representative routes for streaming/memory work; do not impose a full benchmark suite on every minor edit. [ROADMAP.md](ROADMAP.md) tracks current evidence and unresolved measurements.

## Medium-term contract

The Windows baseline target is **2560 × 1440 at 60 FPS on High**, equivalent to a 16.67 ms frame interval. **120 FPS** is a scalable target with an 8.33 ms interval on identified hardware and appropriate quality settings. This supersedes the former 1080p/120 primary target. Balanced and High are the initial presets; Ultra requires a demonstrated visual benefit. No reference Windows CPU/GPU has been identified in the foundation review. Record that hardware before accepting a performance gate. On desktop, record display synchronization and the chosen target cadence; native display refresh is not automatically the performance profile.

This is a target for named hardware, resolution, quality settings, and representative scenarios. It is not a promise for every PC or for an empty scene. Measure standalone players on the target OS; Editor Play mode is diagnostic only.

## Repeatable scenarios

When investigating a relevant bottleneck or preparing certification, reuse existing deterministic scenarios for idle baseline, ten-enemy combat with effects and loot, repeated harvesting and inventory changes, populated homestead construction, continuous chunk traversal, camp travel, rain/water and roof changes. Dungeon entry/return belongs to the later dungeon task. Use seed 42 for matched visual/performance comparisons and the [world reference](PROCEDURAL_WORLDS.md#diagnostics-and-authoring) for integration seeds. For a matched benchmark or certification run, record a cold pass, then at least three warmed circuits in a controlled display mode; an ordinary visual edit does not require this sequence. Record build revision, machine CPU/GPU/RAM/OS, graphics API, build type, resolution, render scale, refresh, target cadence, VSync/frame cap, quality preset, duration, p50/p95/p99 and longest frame, counts above 16.67 and 33.33 ms, CPU/GPU timing where supported, and memory. Keep scenario reports outside Git; update the owning reference only when a durable decision changes; summarize routine comparisons in chat.

For streaming, record stage timings, activation/source-collection spikes, pending work,
loaded object/resource counts and peak memory per circuit. Stable repeated routes must
reach bounded residency rather than grow each lap. Attribute GPU costs for shadows,
AO, atmosphere, vegetation and water where tools support it; mark unavailable counters
honestly. CPU/GPU work overlaps, so do not sum the two into an invented whole-frame time.
The streamer targets roughly 1 ms population slices during traversal, with larger batches only behind entry/loading. This is a scheduling target, not a demonstrated whole-frame budget; native calls, GPU work and source collection must be measured separately.

The bootstrap player can write a JSON diagnostic report with `--topaz-perf`. Keep its window active: it warms up for 10 seconds, samples for 60 uninterrupted foreground seconds, and restarts if focus is lost. Reports save under `Application.persistentDataPath/PerformanceReports`. The report uses Unity frame deltas. Use a native Windows presentation-timing capture as the final frame-pacing check, and the Unity Profiler/Frame Debugger/Memory Profiler to explain bottlenecks. Measurement tools can add overhead; compare like builds and capture conditions.

The continuous traversal diagnostic is `--topaz-smoke --topaz-traversal --topaz-traversal-seconds=180 --topaz-smoke-quit`, with an absolute `--topaz-capture-dir`. It drives the real controller without capture IO inside the sample window and reports readiness stops, interval percentiles, terrain commits and navigation submission. Batch mode is useful for CPU-side readiness/streaming diagnostics, but its uncapped loop intervals are not rendered frame pacing. Foreground testing requires an unlocked, awake display; offscreen review images cannot establish a stutter-free player.

## Optimization order

1. Reproduce a specific hitch or budget miss in a standalone build.
2. Identify CPU, GPU, memory, I/O, asset import, shader compilation, or presentation pacing as the limiting cause.
3. Make the smallest targeted change, rerun the same scenario, and check that visual quality and input feel remain acceptable.

Use scripted stress scenarios and real-device measurement before adopting architecture for a larger entity count. Benchmark loading and first-use effects as well as steady-state frames; avoid synchronous scene loads during interactive play.

## Dense foliage

Keep grass in native instanced Terrain details; do not create a GameObject per blade. Unity's [instanced detail path](https://docs.unity.com/en-us/engine/6000.6/manual/creating-environments/script-terrain/terrain-grass) already supports custom materials and persistent instance data. Moving placement into a compute shader does not by itself reduce shaded pixels, wind vertices, shadow work or GI cost. A proposed indirect renderer must demonstrate additional per-instance culling or mesh-LOD savings with equal density, materials, wind history, shadows and construction suppression before replacing this path.

Use artist-authored reduced meshes and effective tree LODs, native signed crossfades, stable grass distance thinning and a shared visibility envelope. A material must not force `LOD_FADE_CROSSFADE` on Terrain details, which have no LODGroup fade factor. Keep character and foliage motion passes enabled for temporal rendering. The owned foliage shader specializes unused emission/frost/pulse/backlighting features while retaining the active palette, normals and wind.

Benchmark native depth priming and GPU occlusion instead of assuming they improve every GPU. Unity documents [depth priming's overdraw tradeoff with GPU Resident Drawer](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer-performance); verify opaque shader depth passes and visible grass as well as timings. `--topaz-depth-priming` and `--topaz-no-depth-priming` provide a controlled traversal comparison; `--topaz-gpu-occlusion` is a separate control. A faster image missing vegetation is a failed rendering experiment, not an optimization result.

Scripted traversal fixes its camera heading, zoom and pitch independently of physical mouse input, while retaining Cinemachine following/collision. Receipts record the camera pose, native GI preset, shadow budget, DoF and actual render-scale range. `--topaz-stp` is an isolated adaptive-resolution comparison. `--topaz-balanced` selects the isolated Balanced comparison; normal saved preferences are untouched. `--topaz-traversal-circuits=4` repeats the first 240 m of the route out and back, recording one cold and three warm circuits under held time and clear weather. Memory fields are Unity allocated bytes, with peaks sampled every 60 frames rather than process resident-memory measurements. Motion-capture runs are explicitly instrumented and must not establish performance acceptance. An empty `gpuPasses` list means GPU marker timings were unavailable; total GPU frame timing and controlled ablations remain distinct evidence.

## Unity references

Use the Unity 6.6 [Profiler](https://docs.unity3d.com/6000.6/Documentation/Manual/Profiler.html) and [player data collection guide](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-profiling-applications.html) to investigate CPU and memory cost. Use the [Frame Debugger](https://docs.unity3d.com/6000.6/Documentation/Manual/FrameDebugger.html) or [URP Render Graph Viewer](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/render-graph-view.html) for rendering cost. The [reference index](UNITY_REFERENCE_GUIDE.md) identifies the installed documentation baseline. These tools explain measurements; the standalone scenario process above remains the performance gate.
