using BepInEx;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using NonStopHallsGauntlet;

public class OverlayEnemies : MonoBehaviour
{
    public static OverlayEnemies Instance;

    private Dictionary<string, Texture2D> overlayTextures = new();
    private List<string> activeOverlays = new();
    private Dictionary<string, string> overlayTexts = new();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadOverlay("Reed", "NonStopHallsGauntlet/Reed.png");
        LoadOverlay("Choristor", "NonStopHallsGauntlet/Choristor.png");
        LoadOverlay("Bellringer", "NonStopHallsGauntlet/Bellringer.png");
        LoadOverlay("Clawmaiden", "NonStopHallsGauntlet/Clawmaiden.png");
        LoadOverlay("Bellbearer", "NonStopHallsGauntlet/Bellbearer.png");
        LoadOverlay("Admin", "NonStopHallsGauntlet/Admin.png");
        LoadOverlay("Maestro", "NonStopHallsGauntlet/Maestro.png");
        LoadOverlay("Sentry", "NonStopHallsGauntlet/Sentry.png");
    }

    public void SetOverlayText(string key, string text)
    {
        if (!overlayTextures.ContainsKey(key))
        {
            return;
        }

        overlayTexts[key] = text;
    }
    public void LoadOverlay(string key, string relativePath)
    {
        string path = Path.Combine(Paths.PluginPath, relativePath);

        if (!File.Exists(path))
        {
            return;
        }

        byte[] data = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(tex, data);

        overlayTextures[key] = tex;
    }

    public void TriggerOverlay(string key)
    {
        if (!overlayTextures.ContainsKey(key)) return;
        if (!activeOverlays.Contains(key))
            activeOverlays.Add(key);
    }

    public void RemoveOverlay(string key)
    {
        activeOverlays.Remove(key);
    }


    public void ClearAllOverlays()
    {
        activeOverlays.Clear();
    }

    void OnGUI()
    {
        if (activeOverlays.Count == 0) return;

        List<string> keys = new(activeOverlays);

        float totalWidth = 0f;
        foreach (string key in keys)
        {
            Texture2D tex = overlayTextures[key];
            totalWidth += tex.width * Plugin.scale.Value;
        }
        totalWidth += Plugin.gap.Value * (keys.Count - 1);

        float x = (Screen.width - totalWidth) / 2f + Plugin.posX.Value;

        float baselineY = Plugin.posY.Value;

        foreach (string key in keys)
        {
            Texture2D tex = overlayTextures[key];
            float w = tex.width * Plugin.scale.Value;
            float h = tex.height * Plugin.scale.Value;

            float drawY = baselineY - h;

            GUI.DrawTexture(new Rect(x, drawY, w, h), tex);

            if (overlayTexts.TryGetValue(key, out string label))
            {
                int fontSize = 30;
                fontSize = Mathf.RoundToInt(fontSize * Plugin.scale.Value);
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = Color.white }
                };

                Rect textRect = new Rect(x, drawY + h + 2f, w, fontSize + Mathf.RoundToInt(20 * Plugin.scale.Value));
                GUI.Label(textRect, label, style);
            }
            x += w + Plugin.gap.Value;
        }

    }
}
