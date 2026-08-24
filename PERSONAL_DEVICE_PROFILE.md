# Personal IdeaPad build profile

This branch exists for one machine: a Lenovo IdeaPad Gaming 3 15IHU6 running Windows 11 x64.

## Hardware baseline

| Component | Target |
| --- | --- |
| CPU | Intel Core i5-11300H (4C/8T) |
| RAM | 8 GB DDR4-3200 |
| iGPU | Intel Iris Xe Graphics |
| dGPU | NVIDIA GeForce GTX 1650 4 GB |
| Displays | 1920x1080 at 60 Hz |
| OS | Windows 11 x64 |

No serial number or other unique private device identifier belongs in this repository.

## What this branch may intentionally sacrifice

- x86 and ARM32 builds
- Windows Mobile / Xbox support
- old Windows compatibility
- high-refresh-specific behavior
- generalized hardware tuning for machines other than this IdeaPad

Required UWP packaging metadata may remain even when it references a legacy platform concept.

## What it should not sacrifice

- credential security
- correct Discord behavior
- DPI/resizable-window support
- native Fluent/Windows 11 interaction quality
- maintainable code

## Optimization order

1. Reduce unnecessary persistent RAM use.
2. Preserve list virtualization and incremental message loading.
3. Avoid waking the GTX 1650 for basic chat UI when possible.
4. Keep scrolling/channel switching responsive at 1080p60.
5. Prefer native WinUI/UWP surfaces over embedded web UI.
6. Measure before adding machine-specific micro-optimizations.

## Quick test pass

After a meaningful build, verify:

- launch/login path still works;
- normal DMs and guild channels load;
- message context menu + hover actions work;
- message grouping remains correct after deletion/insertion;
- official Discord notification DMs remain read-only with the security warning;
- RAM does not continuously climb while switching channels;
- ordinary text chat does not unnecessarily activate the GTX 1650;
- UI remains smooth on the 1920x1080 60 Hz display.
