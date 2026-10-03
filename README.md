# Strafer

A fast-paced first-person movement shooter built in Unity with C#.

Players fight with a semi-automatic hand cannon and a metal whip, using four short-cooldown abilities to outmaneuver each other.

This is the Unity port of the original Unreal Engine 5.8 C++ version ([guccineonicon/strafer](https://github.com/guccineonicon/strafer)). Tuning values and movement physics carry over one to one.

## Requirements

- Unity 6.6 (6000.6)
- [Git LFS](https://git-lfs.com) for binary assets (`git lfs install` once per machine)

## Getting started

1. Clone the repository and run `git lfs pull`.
2. In Unity Hub, choose **Add > Add project from disk** and pick the repository folder. Open it with Unity 6.6.
3. If Unity asks to enable the new Input System backends, click **Yes**. The editor restarts.
4. Open any scene (a new empty scene works) and press **Play**.

No setup is needed. When a scene has no game mode, Strafer starts a match automatically: it builds a practice arena if there is no ground, spawns you and three practice dummies, and takes over the camera. See [docs/Setup.md](docs/Setup.md) for controls and customization.

## Project layout

```
Assets/Strafer/Scripts/
  Core/         Game mode, auto-start, spawn points, practice arena, shared helpers
  Characters/   Character, movement motor, health, player input
  Abilities/    Dash, roll, deflect, and stun
  Weapons/      Hand cannon
  Controls/     Code-built input actions and key rebinding
  Settings/     Mouse sensitivity and saved settings
  UI/           HUD and settings menu
  Visuals/      Placeholder body, first-person viewmodel, debug drawing, speed blur
  URP/          Optional motion blur, compiled only when URP is installed
docs/           Setup guide
```

## Gameplay summary

| Ability | Cooldown | Effect |
|---|---|---|
| Dash | 2 s | 8 m burst at 640 UPS in the input direction. Invulnerable while dashing. |
| Roll | 2 s | 6 m burst at 460 UPS in the input direction. |
| Deflect | 2 s | Opens a 0.5 s window. Hitscan shots that land during it are reflected to the shooter. |
| Stun | 3 s | Whip strike that stuns the first enemy within 4 m for 1 s. |

- Base movement speed is 360 UPS.
- Hand cannon: 8 rounds, 2.5 shots per second, 1.75 s reload, 50 damage. Health is 100.
- Starting a reload resets all ability cooldowns. Abilities remain usable while reloading.

Speeds are tuned in Source/Quake units per second (UPS), where 1 unit = 1.905 cm. See `Assets/Strafer/Scripts/Core/StraferUnits.cs`.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for branching and commit conventions.
