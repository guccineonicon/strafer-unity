using Strafer.Abilities;
using Strafer.Controls;
using Strafer.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Strafer.Characters
{
    /// <summary>
    /// Reads the local player's mouse and keyboard and drives the character.
    ///
    /// Gameplay input stops automatically while the settings menu is open,
    /// because the menu disables the Gameplay action map.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class StraferPlayerInput : MonoBehaviour
    {
        private StraferCharacter character;
        private StraferControls controls;

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
            controls = StraferControls.Instance;
        }

        private void Update()
        {
            if (!controls.Gameplay.enabled)
            {
                character.MoveInput = Vector2.zero;
                return;
            }

            UpdateMoveInput();
            UpdateLook();

            if (controls.WasPressed(StraferActionNames.Jump))
            {
                character.Motor.RequestJump();
            }

            // The hand cannon is semi-automatic: one shot per press.
            if (controls.WasPressed(StraferActionNames.Fire))
            {
                character.HandCannon.TryFire();
            }

            if (controls.WasPressed(StraferActionNames.Reload))
            {
                character.HandCannon.TryReload();
            }

            if (controls.WasPressed(StraferActionNames.Dash))
            {
                character.Abilities.TryActivate(StraferAbility.Dash);
            }

            if (controls.WasPressed(StraferActionNames.Deflect))
            {
                character.Abilities.TryActivate(StraferAbility.Deflect);
            }

            if (controls.WasPressed(StraferActionNames.Stun))
            {
                character.Abilities.TryActivate(StraferAbility.Stun);
            }

            if (controls.WasPressed(StraferActionNames.Roll))
            {
                character.Abilities.TryActivate(StraferAbility.Roll);
            }
        }

        private void UpdateMoveInput()
        {
            // Each direction is a separate key so opposite keys cancel out cleanly.
            float right = (controls.IsHeld(StraferActionNames.MoveRight) ? 1f : 0f)
                - (controls.IsHeld(StraferActionNames.MoveLeft) ? 1f : 0f);
            float forward = (controls.IsHeld(StraferActionNames.MoveForward) ? 1f : 0f)
                - (controls.IsHeld(StraferActionNames.MoveBackward) ? 1f : 0f);

            character.MoveInput = new Vector2(right, forward);
        }

        private void UpdateLook()
        {
            // Looking stays available while stunned so the player can track their attacker.
            Mouse mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            // The mouse delta is the raw count of mouse movement since last frame, with
            // no engine-side scaling or smoothing. This makes the sensitivity number map
            // directly to degrees per count.
            Vector2 counts = mouse.delta.ReadValue();
            float degreesPerCount = StraferSettings.DegreesPerCount;
            character.AddLook(counts.x * degreesPerCount, counts.y * degreesPerCount);
        }
    }
}
