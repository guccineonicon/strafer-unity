# Contributing

## Branches

- `main` holds stable, playable builds. Only merge into it from `dev`.
- `dev` is the integration branch. Feature branches merge here first.
- Feature work happens on short-lived branches cut from `dev`:
  - `feature/<name>` for new gameplay or systems
  - `fix/<name>` for bug fixes
  - `docs/<name>` for documentation-only changes

Open a pull request into `dev` when a feature compiles without warnings and has been tested in Play mode.

## Commit messages

```
Short imperative summary under 72 characters

Optional body explaining what changed and why, wrapped at 72
characters. Reference the systems touched and any tuning values
that changed.
```

Examples:
- `Add dash ability with invulnerability window`
- `Fix roll ending early when hitting a slope`

## Unity specifics

- Commit `ProjectSettings/` and every `.meta` file Unity generates alongside its asset. A missing `.meta` breaks references for everyone else.
- Keep scenes and prefabs small and focused so merges stay manageable.
- Binary assets (textures, models, audio, fonts) go through Git LFS. See `.gitattributes`.

## Code style

- Gameplay code lives in `Assets/Strafer/Scripts/<Area>/` under the matching `Strafer.<Area>` namespace.
- One public type per file, named after the file.
- Private fields use `camelCase`; public members and properties use `PascalCase`.
- Expose tuning values as `[SerializeField]` fields with `[Tooltip]`, `[Min]`, or `[Range]` so they can be adjusted in the Inspector.
- Comments describe what the code does and why. Write them for someone reading the file for the first time.
