# Performance process

## Contract

The first gameplay target is **1080p at 120 FPS** on one designated Windows gaming PC, equivalent to an 8.33 ms frame budget. A 60 Hz target has a 16.67 ms budget; choose a lower-tier reference PC once a representative scene exists. On desktop, use display synchronization for even pacing and aim at the display's native refresh rate when hardware permits. Quality settings may trade visual cost for a stable cadence.

This is a target for named hardware, resolution, quality settings, and representative scenarios. It is not a promise for every PC or for an empty scene. Measure standalone players on the target OS; Editor Play mode is diagnostic only.

## Repeatable scenarios

As each system is built, add deterministic scenarios for idle baseline, ten-enemy combat with effects and loot, repeated harvesting and inventory changes, populated homestead construction, region transition, and dungeon entry/return. Run each after warm-up at least three times in a controlled display mode. Record build revision, machine, resolution, refresh, quality preset, duration, p50/p95/p99 and longest frame, counts above 16.67 and 33.33 ms, CPU/GPU timing where supported, and memory. Keep scenario reports outside Git; commit concise comparisons and decisions.

The bootstrap player can write a JSON diagnostic report with `--topaz-perf`. Keep its window active: it warms up for 10 seconds, samples for 60 uninterrupted foreground seconds, and restarts if focus is lost. Reports save under `Application.persistentDataPath/PerformanceReports`. The report uses Unity frame deltas. Use a native Windows presentation-timing capture as the final frame-pacing check, and the Unity Profiler/Frame Debugger/Memory Profiler to explain bottlenecks. Measurement tools can add overhead; compare like builds and capture conditions.

## Optimization order

1. Reproduce a specific hitch or budget miss in a standalone build.
2. Identify CPU, GPU, memory, I/O, asset import, shader compilation, or presentation pacing as the limiting cause.
3. Make the smallest targeted change, rerun the same scenario, and check that visual quality and input feel remain acceptable.

For the planned scale, do not adopt the thousand-entity solutions from Deep Rock Galactic: Survivor by default. Its useful pattern is scripted stress scenarios and real-device measurement. Benchmark loading and first-use effects as well as steady-state frames; avoid synchronous scene loads during interactive play.
