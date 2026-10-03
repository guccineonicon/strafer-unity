# Setup

The game is playable straight after opening the project. Controls, characters, the HUD, and the settings menu are all created in code, so **no scene setup is required**. Open any scene, press **Play**, and you spawn as the Strafer character.

## Default controls

| Action | Key |
|---|---|
| Move | W / A / S / D |
| Look | Mouse |
| Jump | Space |
| Fire | Left Mouse Button |
| Reload | R |
| Dash | Left Shift |
| Deflect | Q |
| Stun (whip) | Right Mouse Button |
| Roll | Left Ctrl |
| Settings menu | P |

In the editor, press **Escape** to release the mouse. Click the Game view to capture it again.

## Settings menu

Press **P** to open the settings menu (P instead of Escape, since Escape releases the cursor in the editor). It has:

- **Mouse sensitivity**: drag the slider or type an exact value (0.01 to 20, default 5). It uses Overwatch's scale (0.0066 degrees per mouse count per point), so your Overwatch sensitivity feels identical here at the same DPI.
- **Keybinds**: click a key and press its replacement. Escape cancels. **Reset Keybinds** restores the defaults.

Sensitivity and key bindings are saved to PlayerPrefs.

## Building a level

When a scene has no ground under the spawn point, a practice arena is built automatically. To use your own level:

1. Build the level geometry with colliders.
2. Add one or more empty GameObjects with a **Strafer Spawn Point** component. The position is where the character's feet go; the blue gizmo arrow shows the facing direction.

The player spawns at the first spawn point and respawns at a random one. With no spawn points, the world origin is used.

## Practice dummies

Three practice dummies spawn 8 m in front of the first spawn point. They take damage, can be stunned, and revive two seconds after dying. To place dummies yourself instead, add **Strafer Character** components to empty GameObjects in the scene; automatic spawning is skipped when any are present.

## Customizing

Add a **Strafer Game Mode** component to a GameObject in your scene to change match settings in the Inspector: respawn delay, dummy count, spacing, and whether to build the practice arena.

To tune a character (movement, abilities, weapon, viewmodel):

1. Create an empty GameObject and add **Strafer Character**.
2. Add any of its components you want to tune: **Strafer Motor**, **Strafer Abilities**, **Hand Cannon**, **Strafer Health**, **Strafer Viewmodel**, **Strafer Body**. Anything you skip is added with default values at runtime.
3. Save it as a prefab and assign it to **Player Prefab** (and optionally **Dummy Prefab**) on the game mode.

### Custom gun model

On the **Strafer Viewmodel** component, assign a prefab or imported model to **Gun Model**. Use **Gun Model Position / Rotation / Scale** to line it up if its pivot or facing differs from +Z forward.

## Speed blur

Dash and roll switch on camera motion blur when the project uses the Universal Render Pipeline (URP). In the built-in render pipeline the effect is skipped. To enable it, install URP from the Package Manager and assign a URP asset under **Project Settings > Graphics**.

## Opting out of auto-start

Add a **Strafer Auto Start Opt Out** component to any GameObject in a scene (for example a main menu) to stop a match from starting there.
