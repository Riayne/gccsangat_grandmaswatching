# CLAUDE.md

Unity mobile game jam project. Final deliverable is an **Android APK**.

## Project
- Unity 6000.6.9f1 (Unity 6.6), 2D URP template.
- Target platform: Android. Build output is an APK, not an AAB.
- Keystores (`*.keystore`, `*.jks`) are gitignored. Never commit signing files or passwords.

## Design constraints
- **Touch controls only.** No keyboard, mouse or gamepad input paths. Design for tap, hold and swipe on a phone screen, in portrait unless decided otherwise.
- **One core mechanic.** Every feature must serve it. If something doesn't, leave it out.
- **Keep scope small.** It's a game jam: get something playable end to end first, then polish. Prefer cutting features over adding complexity.

## Code
- Simple C# MonoBehaviour scripts. One clear responsibility per script.
- No heavy frameworks or architecture (no DI containers, no ECS/DOTS, no complex event buses).
- **Avoid heavy packages.** Use what's already in `Packages/manifest.json`. Ask before adding any package or Asset Store asset.
- Keep scripts under `Assets/Scripts/`, scenes under `Assets/Scenes/`.
- Always keep `.meta` files with their assets.

## Git workflow
- Commit after each working feature, with a clear message describing what it adds (e.g. `Add tap-to-jump player control`).
- Only commit features that work: compiles without errors and has been tested in the Editor.
- Don't commit `Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/` or build output (already covered by `.gitignore`).
