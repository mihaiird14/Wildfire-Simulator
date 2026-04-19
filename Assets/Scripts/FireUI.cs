using UnityEngine;

public class FireUI : MonoBehaviour
{
    public FireSimulator fireSimulator;

    private bool _popupVisible = true;
    private int _dirIndex = 0;
    private int _strengthIndex = 1;

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

        // Fundal inchis
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Dimensiuni popup
        float w = 360f;
        float h = 320f;
        float px = (Screen.width - w) / 2f;
        float py = (Screen.height - h) / 2f;

        // Fundal popup
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

        // Label directie
        GUIStyle lbl = new GUIStyle(GUI.skin.label);
        lbl.fontSize = 13;
        lbl.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x, y, cw, 22f), "Directia vantului:", lbl);
        y += 26f;

        // Grid 3x3 directii
        string[] grid = {
            "NV", "N", "NE",
            "V",  "—", "E",
            "SV", "S", "SE"
        };
        int[] gridIdx = { 6, 1, 5, 4, 0, 3, 8, 2, 7 };

        float btnW = (cw - 8f) / 3f;
        float btnH = 30f;

        GUIStyle btn = new GUIStyle(GUI.skin.button);
        btn.fontSize = 13;

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

        // Label intensitate
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
        y += btnH + 16f;

        // Buton start
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
        fireSimulator.enabled = true;

        Debug.Log("[FireUI] Vant: " + _dirNames[_dirIndex] +
                  " | Intensitate: " + _strengthNames[_strengthIndex]);
    }
}