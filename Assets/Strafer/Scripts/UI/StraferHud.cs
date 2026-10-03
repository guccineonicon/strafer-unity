using Strafer.Abilities;
using Strafer.Characters;
using Strafer.Weapons;
using UnityEngine;

namespace Strafer.UI
{
    /// <summary>
    /// Minimal testing HUD drawn with Unity's immediate-mode GUI: crosshair,
    /// health, ammo, reload progress, ability cooldowns, kills, and a stun warning.
    /// </summary>
    public class StraferHud : MonoBehaviour
    {
        private const float CrosshairSize = 6f;
        private const float CrosshairThickness = 2f;
        private const float TextMargin = 24f;
        private const float LineHeight = 20f;
        private const int FontSize = 15;

        private static readonly string[] AbilityLabels = { "Dash", "Deflect", "Stun", "Roll" };

        private StraferCharacter character;
        private GUIStyle textStyle;
        private GUIStyle shadowStyle;

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
        }

        private void OnGUI()
        {
            if (character == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();
            DrawCrosshair();
            DrawStatusText();
        }

        private void DrawCrosshair()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float half = CrosshairThickness * 0.5f;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(centerX - CrosshairSize, centerY - half, CrosshairSize * 2f, CrosshairThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - half, centerY - CrosshairSize, CrosshairThickness, CrosshairSize * 2f), Texture2D.whiteTexture);
        }

        private void DrawStatusText()
        {
            // Health, ammo, four abilities, and kills.
            const int lineCount = 7;
            float y = Screen.height - TextMargin - LineHeight * lineCount;

            StraferHealth health = character.Health;
            DrawLine(ref y, string.Format("Health  {0:0} / {1:0}", health.CurrentHealth, health.MaxHealth), Color.white);

            HandCannon handCannon = character.HandCannon;
            string ammoText = handCannon.IsReloading
                ? string.Format("Reloading  {0:0}%", handCannon.ReloadProgress * 100f)
                : string.Format("Ammo  {0} / {1}", handCannon.CurrentAmmo, handCannon.MagazineSize);
            DrawLine(ref y, ammoText, Color.white);

            StraferAbilities abilities = character.Abilities;
            for (int i = 0; i < StraferAbilities.AbilityCount; i++)
            {
                float remaining = abilities.GetCooldownRemaining((StraferAbility)i);
                bool ready = remaining <= 0f;
                DrawLine(ref y,
                    ready ? AbilityLabels[i] + "  READY" : string.Format("{0}  {1:0.0}s", AbilityLabels[i], remaining),
                    ready ? Color.green : Color.gray);
            }

            DrawLine(ref y, "Kills  " + character.Score, Color.yellow);

            if (abilities.IsStunned)
            {
                DrawText(new Rect(Screen.width * 0.5f - 40f, Screen.height * 0.5f + 40f, 200f, LineHeight), "STUNNED", Color.red);
            }
        }

        private void DrawLine(ref float y, string text, Color color)
        {
            DrawText(new Rect(TextMargin, y, 400f, LineHeight), text, color);
            y += LineHeight;
        }

        private void DrawText(Rect rect, string text, Color color)
        {
            // A one-pixel drop shadow keeps the text readable over bright scenery.
            Rect shadowRect = rect;
            shadowRect.x += 1f;
            shadowRect.y += 1f;
            GUI.Label(shadowRect, text, shadowStyle);

            textStyle.normal.textColor = color;
            GUI.Label(rect, text, textStyle);
        }

        private void EnsureStyles()
        {
            if (textStyle != null)
            {
                return;
            }

            textStyle = new GUIStyle(GUI.skin.label) { fontSize = FontSize };
            shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }
    }
}
