using System.Globalization;
using Strafer.Characters;
using Strafer.Controls;
using Strafer.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Strafer.UI
{
    /// <summary>
    /// In-game settings menu with mouse sensitivity and key rebinding, drawn with
    /// Unity's immediate-mode GUI.
    ///
    /// Opened with the Menu action (P by default) rather than Escape, because
    /// Escape releases the cursor in the Unity editor. While open, gameplay input
    /// is disabled and the cursor is freed. Settings are saved when it closes.
    /// </summary>
    public class StraferSettingsMenu : MonoBehaviour
    {
        private const float PanelWidth = 560f;
        private const float MaxPanelHeight = 760f;
        private const float PanelPadding = 28f;
        private const string SensitivityFieldName = "StraferSensitivityField";

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.05f, 0.95f);
        private static readonly Color HintColor = new Color(0.6f, 0.6f, 0.6f);

        private StraferCharacter character;
        private StraferControls controls;

        private Vector2 keybindScroll;
        private string sensitivityText = string.Empty;
        private string rebindingAction;

        // Key presses on this frame are ignored, so the key that finished or
        // cancelled a rebind (often Escape) does not also close the menu.
        private int ignoreKeysOnFrame = -1;

        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle hintStyle;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
            controls = StraferControls.Instance;
        }

        private void Start()
        {
            // Capture the mouse straight away so aiming works without clicking first.
            LockCursor();
        }

        private void OnDisable()
        {
            if (IsOpen)
            {
                Close();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !IsOpen)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            if (controls.IsRebinding || Time.frameCount == ignoreKeysOnFrame)
            {
                return;
            }

            if (!IsOpen)
            {
                if (controls.WasPressed(StraferActionNames.Menu))
                {
                    Open();
                }

                return;
            }

            Keyboard keyboard = Keyboard.current;
            bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            if (escapePressed || controls.WasPressed(StraferActionNames.Menu))
            {
                Close();
            }
        }

        /// <summary>Opens the menu if closed, or closes it if open.</summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (IsOpen || character == null || character.ViewCamera == null)
            {
                return;
            }

            IsOpen = true;
            sensitivityText = FormatSensitivity(StraferSettings.MouseSensitivity);

            // Disabling gameplay actions releases held keys and stops the character
            // moving, looking, or firing while the menu is open.
            controls.Gameplay.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            rebindingAction = null;
            StraferSettings.Save();

            controls.Gameplay.Enable();
            LockCursor();
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            // Draw above the HUD.
            GUI.depth = -10;
            EnsureStyles();

            GUI.color = BackdropColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float panelHeight = Mathf.Min(MaxPanelHeight, Screen.height - 40f);
            Rect panel = new Rect((Screen.width - PanelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, PanelWidth, panelHeight);
            GUI.color = PanelColor;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            Rect content = new Rect(panel.x + PanelPadding, panel.y + PanelPadding,
                panel.width - PanelPadding * 2f, panel.height - PanelPadding * 2f);
            GUILayout.BeginArea(content);

            GUILayout.Label("Settings", titleStyle);
            GUILayout.Space(16f);

            DrawSensitivity();
            GUILayout.Space(24f);

            DrawKeybinds();
            GUILayout.Space(16f);

            DrawButtons();

            GUILayout.EndArea();
        }

        private void DrawSensitivity()
        {
            // Mouse sensitivity: drag the slider or type an exact value.
            GUILayout.Label("Mouse", headerStyle);
            GUILayout.Space(6f);

            float sensitivity = StraferSettings.MouseSensitivity;

            // Keep the text box in step with the slider unless the player is typing in it.
            if (GUI.GetNameOfFocusedControl() != SensitivityFieldName)
            {
                sensitivityText = FormatSensitivity(sensitivity);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sensitivity", bodyStyle, GUILayout.Width(100f));

            float sliderValue = GUILayout.HorizontalSlider(sensitivity,
                StraferSettings.MinMouseSensitivity, StraferSettings.MaxMouseSensitivity, GUILayout.ExpandWidth(true));
            if (!Mathf.Approximately(sliderValue, sensitivity))
            {
                StraferSettings.MouseSensitivity = sliderValue;
                sensitivityText = FormatSensitivity(StraferSettings.MouseSensitivity);
            }

            GUILayout.Space(12f);
            GUI.SetNextControlName(SensitivityFieldName);
            string typed = GUILayout.TextField(sensitivityText, GUILayout.Width(80f));
            if (typed != sensitivityText)
            {
                sensitivityText = typed;
                float parsed;
                if (float.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    StraferSettings.MouseSensitivity = parsed;
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawKeybinds()
        {
            // Key bindings: click a key, then press the replacement.
            GUILayout.Label("Keybinds", headerStyle);
            GUILayout.Label("Click a key, then press the new key. Esc cancels.", hintStyle);
            GUILayout.Space(6f);

            keybindScroll = GUILayout.BeginScrollView(keybindScroll, GUILayout.ExpandHeight(true));

            int count = StraferControls.RebindableActions.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                string actionName = StraferControls.RebindableActions[i, 0];
                string label = StraferControls.RebindableActions[i, 1];

                GUILayout.BeginHorizontal();
                GUILayout.Label(label, bodyStyle, GUILayout.ExpandWidth(true));

                bool waiting = rebindingAction == actionName;
                string keyText = waiting ? "Press a key..." : controls.GetBindingDisplayName(actionName);

                GUI.enabled = !controls.IsRebinding;
                if (GUILayout.Button(keyText, GUILayout.Width(180f), GUILayout.Height(26f)))
                {
                    StartRebind(actionName);
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUILayout.Space(3f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawButtons()
        {
            GUI.enabled = !controls.IsRebinding;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Keybinds", GUILayout.Width(140f), GUILayout.Height(30f)))
            {
                controls.ResetAllBindings();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Close", GUILayout.Width(100f), GUILayout.Height(30f)))
            {
                Close();
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void StartRebind(string actionName)
        {
            rebindingAction = actionName;
            controls.StartRebind(actionName, () =>
            {
                rebindingAction = null;
                ignoreKeysOnFrame = Time.frameCount;
            });
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static string FormatSensitivity(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            hintStyle = new GUIStyle(bodyStyle);
            hintStyle.normal.textColor = HintColor;
        }
    }
}
