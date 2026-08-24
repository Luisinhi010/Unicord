# Personal build instructions — IdeaPad Gaming 3 15IHU6

This branch is a private, single-device build of Unicord. Optimize for the target notebook below instead of preserving broad platform compatibility.

## Target hardware

- Device: Lenovo IdeaPad Gaming 3 15IHU6
- OS: Windows 11 x64 (current machine is Windows 11 Home Single Language 25H2)
- CPU: Intel Core i5-11300H, 4 cores / 8 threads
- RAM: 8 GB DDR4-3200
- Integrated GPU: Intel Iris Xe Graphics
- Discrete GPU: NVIDIA GeForce GTX 1650 4 GB
- Display target: 1920x1080 at 60 Hz (internal and external displays)

Never add a serial number, account identifier, token, machine secret, or other private identifier to the repository.

## Priorities

1. Memory efficiency is the primary performance constraint. Avoid unbounded caches, eager loading of large collections, duplicate decoded images, unnecessary background processes, and retaining channel/message UI objects after navigation.
2. Keep normal text/chat UI light enough to use the integrated Intel GPU. Do not deliberately wake or require the GTX 1650 for ordinary navigation, messages, Fluent surfaces, or animations unless a measured feature genuinely benefits from it.
3. Target smooth 60 Hz interaction. Do not add work solely for high-refresh displays. Prefer short, lightweight Fluent transitions over expensive continuous effects.
4. Preserve Fluent Design and native Windows behavior. This is not a reason to disable good UI. Prefer efficient WinUI 2/UWP controls, native composition, mouse/keyboard-first interactions, right-click context menus, hover actions, tooltips, and Windows 11 conventions.
5. Optimize layouts for a 1920x1080 desktop window while remaining DPI-aware and resizable. Touch support is secondary for this branch.
6. Build Release x64 only. Do not spend engineering effort on x86, ARM32, Windows Mobile, Xbox, or old Windows versions unless the legacy UWP packaging toolchain currently requires metadata for successful packaging.
7. Treat legacy UWP/MSIX packaging quirks as temporary technical debt, not permanent requirements. Keep a quirk only while it is proven necessary for a successful build/package/install; actively look for a simpler supported replacement and remove obsolete compatibility scaffolding when safe.

## Packaging/toolchain cleanup

The current packaging path is allowed to be ugly only until we can prove a cleaner path works.

When touching the build system:

- Question every legacy manifest field, compatibility target, project indirection, SDK pin, workaround, signing step, and full-solution build requirement.
- Prefer the smallest supported Windows 11 x64 packaging path that still produces a reliably installable package.
- If a workaround exists only because of an old SDK/toolchain behavior, test whether the current toolchain still needs it before preserving it.
- Isolate unavoidable legacy pieces so they do not leak into application code or force unnecessary runtime compatibility work.
- Prefer building only the projects actually required for the desktop package when that is reliable.
- Avoid downloading/installing SDK components during CI when the runner already provides a compatible supported version; if a specific SDK is truly required, document the reason.
- Keep signing secure, but simplify certificate/package plumbing whenever Windows and GitHub Actions permit it.
- Do not remove a weird-looking requirement merely because it looks obsolete: first test build, package creation, installation, launch, and update behavior.
- When a piece of packaging jank survives investigation, leave a short comment explaining exactly why it is still necessary and what would allow us to delete it later.

The goal is not to rewrite UWP for sport. The goal is to progressively reduce accidental complexity around the one Windows 11 x64 build this branch actually needs.

## Security rules

Security does not become optional because this is a personal build.

- Never log, commit, display, or save raw Discord user tokens in plain text.
- Do not add secrets to LocalSettings, source files, GitHub Actions logs, artifacts, or temporary files.
- Prefer supported Discord authentication and Windows Credential Locker/PasswordVault for credentials.
- Treat the secure OAuth/Social SDK work as the direction for authentication; do not reintroduce token-extraction UX.

## Performance guidance

When changing message rendering, media, navigation, or caches:

- Load incrementally rather than preloading entire histories.
- Bound caches and release resources after channel/server switches.
- Avoid decoding media at a resolution much larger than its rendered size when practical.
- Avoid creating one permanent heavy visual/composition object per message when a lightweight template/state is enough.
- Prefer virtualization-friendly controls and do not accidentally disable list virtualization.
- Avoid polling/background loops when an event-driven path exists.
- Do not trade a large persistent RAM increase for a tiny latency improvement without measuring it.

## Validation on the target notebook

For meaningful UI/performance changes, test on the IdeaPad and check:

- idle and active-chat RAM usage in Task Manager;
- CPU while idle and while scrolling;
- GPU engine used by Unicord (ordinary chat should preferably stay on the iGPU);
- smoothness at 1920x1080 / 60 Hz;
- channel switching after long sessions for leaked memory/resources;
- behavior with the GTX 1650 otherwise idle.

Treat memory and GPU targets as measurements, not fake hard guarantees. Prefer regressions that can be observed and reproduced over speculative micro-optimizations.

## Product direction

This branch is allowed to be opinionated and device-specific. Favor the actual owner's workflow over universal configurability, but keep changes understandable and maintainable. When a machine-specific optimization is non-obvious, leave a short comment explaining what constraint it addresses.
