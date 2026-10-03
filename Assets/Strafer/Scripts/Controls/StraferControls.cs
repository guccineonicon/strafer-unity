using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Strafer.Controls
{
    /// <summary>
    /// Builds Strafer's input actions in code and handles runtime key rebinding.
    ///
    /// Every action is a button with one binding, so each movement direction can
    /// be rebound independently. Overrides are saved to PlayerPrefs as JSON and
    /// reloaded on startup.
    ///
    /// Actions live in two maps:
    /// - Gameplay: movement, combat, and abilities. Disabled while a menu is open.
    /// - Interface: the settings menu key. Stays enabled so the menu can be toggled.
    ///
    /// Mouse look is not an action. <see cref="Strafer.Characters.StraferPlayerInput"/>
    /// reads raw mouse counts directly so sensitivity maps exactly to degrees per count.
    /// </summary>
    public sealed class StraferControls
    {
        private const string OverridesKey = "Strafer.BindingOverrides";

        private static StraferControls instance;

        /// <summary>Shared controls for the local player. Created on first use.</summary>
        public static StraferControls Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StraferControls();
                }

                return instance;
            }
        }

        /// <summary>Display labels for each rebindable action, in the order they appear in the settings menu.</summary>
        public static readonly string[,] RebindableActions =
        {
            { StraferActionNames.MoveForward, "Move Forward" },
            { StraferActionNames.MoveBackward, "Move Backward" },
            { StraferActionNames.MoveLeft, "Move Left" },
            { StraferActionNames.MoveRight, "Move Right" },
            { StraferActionNames.Jump, "Jump" },
            { StraferActionNames.Fire, "Fire" },
            { StraferActionNames.Reload, "Reload" },
            { StraferActionNames.Dash, "Dash" },
            { StraferActionNames.Deflect, "Deflect" },
            { StraferActionNames.Stun, "Stun (Whip)" },
            { StraferActionNames.Roll, "Roll" },
            { StraferActionNames.Menu, "Settings Menu" },
        };

        private readonly InputActionAsset asset;
        private InputActionRebindingExtensions.RebindingOperation activeRebind;

        public InputActionMap Gameplay { get; private set; }
        public InputActionMap Interface { get; private set; }

        /// <summary>True while waiting for the player to press a new key for a binding.</summary>
        public bool IsRebinding { get { return activeRebind != null; } }

        private StraferControls()
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "StraferControls";

            Gameplay = asset.AddActionMap("Gameplay");
            AddButton(Gameplay, StraferActionNames.MoveForward, "<Keyboard>/w");
            AddButton(Gameplay, StraferActionNames.MoveBackward, "<Keyboard>/s");
            AddButton(Gameplay, StraferActionNames.MoveLeft, "<Keyboard>/a");
            AddButton(Gameplay, StraferActionNames.MoveRight, "<Keyboard>/d");
            AddButton(Gameplay, StraferActionNames.Jump, "<Keyboard>/space");
            AddButton(Gameplay, StraferActionNames.Fire, "<Mouse>/leftButton");
            AddButton(Gameplay, StraferActionNames.Reload, "<Keyboard>/r");
            AddButton(Gameplay, StraferActionNames.Dash, "<Keyboard>/leftShift");
            AddButton(Gameplay, StraferActionNames.Deflect, "<Keyboard>/q");
            AddButton(Gameplay, StraferActionNames.Stun, "<Mouse>/rightButton");
            AddButton(Gameplay, StraferActionNames.Roll, "<Keyboard>/leftCtrl");

            // P rather than Escape, because Escape releases the cursor in the Unity editor.
            Interface = asset.AddActionMap("Interface");
            AddButton(Interface, StraferActionNames.Menu, "<Keyboard>/p");

            string savedOverrides = PlayerPrefs.GetString(OverridesKey, string.Empty);
            if (!string.IsNullOrEmpty(savedOverrides))
            {
                asset.LoadBindingOverridesFromJson(savedOverrides);
            }

            asset.Enable();
        }

        /// <summary>Finds an action by name in either map. Returns null if no action has that name.</summary>
        public InputAction Find(string actionName)
        {
            return asset.FindAction(actionName, false);
        }

        /// <summary>True while the action's key is held. False if the action's map is disabled.</summary>
        public bool IsHeld(string actionName)
        {
            InputAction action = Find(actionName);
            return action != null && action.enabled && action.IsPressed();
        }

        /// <summary>True on the frame the action's key was pressed. False if the action's map is disabled.</summary>
        public bool WasPressed(string actionName)
        {
            InputAction action = Find(actionName);
            return action != null && action.enabled && action.WasPressedThisFrame();
        }

        /// <summary>Human-readable name of the key currently bound to an action, for display in menus.</summary>
        public string GetBindingDisplayName(string actionName)
        {
            InputAction action = Find(actionName);
            return action != null ? action.GetBindingDisplayString(0) : string.Empty;
        }

        /// <summary>
        /// Waits for the player to press a key, then binds it to <paramref name="actionName"/>.
        /// Escape cancels. All actions are disabled while waiting so the key press
        /// does not also trigger gameplay or close the menu.
        /// </summary>
        /// <param name="onFinished">Called when the rebind completes or is cancelled.</param>
        public void StartRebind(string actionName, Action onFinished)
        {
            InputAction action = Find(actionName);
            if (action == null || IsRebinding)
            {
                return;
            }

            bool gameplayWasEnabled = Gameplay.enabled;
            asset.Disable();

            Action<InputActionRebindingExtensions.RebindingOperation> finish = operation =>
            {
                operation.Dispose();
                activeRebind = null;

                Interface.Enable();
                if (gameplayWasEnabled)
                {
                    Gameplay.Enable();
                }

                SaveOverrides();
                if (onFinished != null)
                {
                    onFinished();
                }
            };

            activeRebind = action.PerformInteractiveRebinding(0)
                .WithExpectedControlType("Button")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Pointer>/press")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(finish)
                .OnCancel(finish)
                .Start();
        }

        /// <summary>Restores every binding to its default key and saves the result.</summary>
        public void ResetAllBindings()
        {
            asset.RemoveAllBindingOverrides();
            SaveOverrides();
        }

        private void SaveOverrides()
        {
            PlayerPrefs.SetString(OverridesKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        private static void AddButton(InputActionMap map, string actionName, string defaultBinding)
        {
            map.AddAction(actionName, InputActionType.Button, defaultBinding, expectedControlLayout: "Button");
        }

        // Static state survives between Play sessions when domain reload is disabled,
        // so rebuild the controls at the start of each session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }
    }
}
