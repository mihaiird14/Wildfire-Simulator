using UnityEngine;

public class FireUI : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    private bool _popupVisible = true;
    private int _dirIndex = 0;
    private int _strengthIndex = 1;
    private float _moisture = 0.1f;  // 0 = uscat, 1 = foarte umed

    private string[] _dirNames = {
        "Fara vant", "Nord", "Sud", "Est", "Vest",
        "Nord-Est",  "Nord-Vest", "Sud-Est", "Sud-Vest"
    };
    private Vector2[] _dirVecs = {
        new Vector2( 0,  0),
        new Vector2( 0,  1),
        new Vector2( 0, -1),
        new Vector2( 1,  0),
        new Vector2(-1,  0),
        new Vector2( 1,  1),
        new Vector2(-1,  1),
        new Vector2( 1, -1),
        new Vector2(-1, -1),
    };

    private string[] _strengthNames = { "Slab", "Mediu", "Puternic" };
    private float[] _strengthVals = { 0.2f, 0.5f, 0.9f };

    void Start()
    {
        if (fireSimulator != null)
            fireSimulator.enabled = false;
    }

    void OnGUI()
    {
        if (!_popupVisible) return;

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 360f;
        float h = 400f;  // marit pentru slider umiditate
        float px = (Screen.width - w) / 2f;
        float py = (Screen.height - h) / 2f;

        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        GUI.DrawTexture(new Rect(px, py, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float x = px + 24f;
        float y = py + 20f;
        float cw = w - 48f;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 18;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, cw, 30f), "Simulare Incendiu", title);
        y += 40f;

        GUIStyle lbl = new GUIStyle(GUI.skin.label);
        lbl.fontSize = 13;
        lbl.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        GUIStyle btn = new GUIStyle(GUI.skin.button);
        btn.fontSize = 13;

        // ── DIRECTIA VANTULUI ──────────────────────────────────
        GUI.Label(new Rect(x, y, cw, 22f), "Directia vantului:", lbl);
        y += 26f;

        string[] grid = { "NV", "N", "NE", "V", "—", "E", "SV", "S", "SE" };
        int[] gridIdx = { 6, 1, 5, 4, 0, 3, 8, 2, 7 };

        float btnW = (cw - 8f) / 3f;
        float btnH = 30f;

        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float bx = x + col * (btnW + 4f);
            float by = y + row * (btnH + 4f);
            bool sel = (_dirIndex == gridIdx[i]);
            GUI.color = sel ? new Color(1f, 0.45f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
            if (GUI.Button(new Rect(bx, by, btnW, btnH), grid[i], btn))
                _dirIndex = gridIdx[i];
        }
        GUI.color = Color.white;
        y += 3 * (btnH + 4f) + 14f;

        // ── INTENSITATE VANT ───────────────────────────────────
        GUI.Label(new Rect(x, y, cw, 22f), "Intensitate vant:", lbl);
        y += 26f;

        float sBtnW = (cw - 8f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            float bx = x + i * (sBtnW + 4f);
            bool sel = (_strengthIndex == i);
            GUI.color = sel ? new Color(0.2f, 0.55f, 1f) : new Color(0.3f, 0.3f, 0.3f);
            if (GUI.Button(new Rect(bx, y, sBtnW, btnH), _strengthNames[i], btn))
                _strengthIndex = i;
        }
        GUI.color = Color.white;
        y += btnH + 14f;

        // ── UMIDITATE ──────────────────────────────────────────
        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(x, y, cw, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y += 10f;

        // Label umiditate cu valoare
        string moistureLabel = GetMoistureLabel(_moisture);
        GUI.Label(new Rect(x, y, cw * 0.6f, 22f), "Umiditate teren:", lbl);

        GUIStyle moistureVal = new GUIStyle(GUI.skin.label);
        moistureVal.fontSize = 13;
        moistureVal.fontStyle = FontStyle.Bold;
        moistureVal.alignment = TextAnchor.MiddleRight;
        moistureVal.normal.textColor = GetMoistureColor(_moisture);
        GUI.Label(new Rect(x + cw * 0.4f, y, cw * 0.6f, 22f), moistureLabel, moistureVal);
        y += 26f;

        // Slider umiditate
        // 0 = uscat (arde usor), 1 = foarte umed (arde greu)
        _moisture = GUI.HorizontalSlider(new Rect(x, y, cw, 20f), _moisture, 0f, 1f);
        y += 24f;

        // Etichete sub slider
        GUIStyle smallLbl = new GUIStyle(GUI.skin.label);
        smallLbl.fontSize = 10;
        smallLbl.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        GUI.Label(new Rect(x, y, 60f, 16f), "Uscat", smallLbl);
        smallLbl.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x + cw - 60f, y, 60f, 16f), "Ploaie", smallLbl);
        y += 20f;

        // ── BUTON START ────────────────────────────────────────
        GUIStyle startBtn = new GUIStyle(GUI.skin.button);
        startBtn.fontSize = 15;
        startBtn.fontStyle = FontStyle.Bold;
        startBtn.normal.textColor = Color.white;

        GUI.color = new Color(0.85f, 0.25f, 0.1f);
        if (GUI.Button(new Rect(x, y, cw, 38f), "Porneste Simularea", startBtn))
        {
            ApplySettings();
            _popupVisible = false;
        }
        GUI.color = Color.white;
    }

    void ApplySettings()
    {
        if (fireSimulator == null) return;

        Vector2 dir = _dirVecs[_dirIndex];
        float strength = _strengthVals[_strengthIndex];

        fireSimulator.windX = dir.x;
        fireSimulator.windZ = dir.y;
        fireSimulator.windStrength = strength;
        fireSimulator.globalMoisture = _moisture;
        fireSimulator.enabled = true;

        Debug.Log("[FireUI] Vant: " + _dirNames[_dirIndex] +
                  " | Intensitate: " + _strengthNames[_strengthIndex] +
                  " | Umiditate: " + (_moisture * 100f).ToString("F0") + "%");
    }

    string GetMoistureLabel(float m)
    {
        if (m < 0.2f) return "Uscat";
        if (m < 0.4f) return "Normal";
        if (m < 0.6f) return "Umed";
        if (m < 0.8f) return "Foarte umed";
        return "Ploaie";
    }

    Color GetMoistureColor(float m)
    {
        return Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(0.3f, 0.6f, 1f), m);
    }
}