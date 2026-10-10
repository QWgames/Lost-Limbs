
using UnityEngine;
using UnityEngine.InputSystem;

public class LLInputDebugOverlay : MonoBehaviour
{
    [Header("Overlay Settings")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [SerializeField] private Vector2 screenPosition = new Vector2(15f, 15f);

    [Header("Display Settings")]
    [SerializeField] private float keySize = 42f;
    [SerializeField] private float spacing = 4f;

    private GUIStyle keyStyle;
    private GUIStyle titleStyle;
    private GUIStyle statusStyle;

    private bool stylesInitialized;

    private void Update()
    {
        // Toggle the overlay with F3.
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            showOverlay = !showOverlay;
        }
    }

    private void OnGUI()
    {
        if (!showOverlay || Keyboard.current == null)
            return;

        InitializeStyles();

        float x = screenPosition.x;
        float y = screenPosition.y;

        // Background panel.
        float panelWidth = keySize * 3f + spacing * 2f + 24f;
        float panelHeight = keySize * 3f + spacing * 3f + 85f;

        GUI.Box(
            new Rect(x, y, panelWidth, panelHeight),
            GUIContent.none
        );

        float contentX = x + 12f;
        float contentY = y + 10f;

        GUI.Label(
            new Rect(contentX, contentY, panelWidth - 24f, 22f),
            "INPUT DEBUG",
            titleStyle
        );

        contentY += 30f;

        // WASD keyboard layout.
        DrawKey("W", Keyboard.current.wKey.isPressed,
            contentX + keySize + spacing, contentY);

        contentY += keySize + spacing;

        DrawKey("A", Keyboard.current.aKey.isPressed,
            contentX, contentY);

        DrawKey("S", Keyboard.current.sKey.isPressed,
            contentX + keySize + spacing, contentY);

        DrawKey("D", Keyboard.current.dKey.isPressed,
            contentX + (keySize + spacing) * 2f, contentY);

        contentY += keySize + spacing + 2f;

        // Shift and Space.
        DrawWideKey("SHIFT",
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed,
            contentX, contentY, keySize * 1.15f);

        DrawWideKey("SPACE",
            Keyboard.current.spaceKey.isPressed,
            contentX + keySize * 1.15f + spacing,
            contentY,
            keySize * 1.85f + spacing);

        contentY += keySize + spacing + 5f;

        GUI.Label(
            new Rect(contentX, contentY, panelWidth - 24f, 20f),
            "Toggle with F3",
            statusStyle
        );
    }




    private void DrawKey(string label, bool pressed, float x, float y)
    {
        DrawKeyBackground(
            label, pressed,
            new Rect(x, y, keySize, keySize)
        );
    }

    private void DrawWideKey(
        string label, bool pressed, float x, float y, float width)
    {
        DrawKeyBackground(
            label, pressed,
            new Rect(x, y, width, keySize)
        );
    }

    private void DrawKeyBackground(
        string label, bool pressed, Rect rect)
    {
        // Save the current IMGUI colors.
        Color previousColor = GUI.color;
        Color previousTextColor = keyStyle.normal.textColor;

        // Use a solid texture so the entire key lights up.
        Color keyColor = pressed
            ? new Color(0.15f, 0.85f, 0.25f, 1f)
            : new Color(0.25f, 0.27f, 0.31f, 1f);

        GUI.color = Color.white;
        GUI.DrawTexture(
            rect,
            Texture2D.whiteTexture,
            ScaleMode.StretchToFill,
            false,
            0f,
            keyColor,
            0f,
            4f
        );

        // Draw the key label on top.
        keyStyle.normal.textColor = Color.white;
        GUI.Label(rect, label, keyStyle);

        // Restore GUI settings.
        GUI.color = previousColor;
        keyStyle.normal.textColor = previousTextColor;
    }




    private void InitializeStyles()
    {
        if (stylesInitialized)
            return;

        keyStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };

        stylesInitialized = true;
    }
}
