using UnityEngine;
using System.Collections.Generic;

public class FireStatistics : MonoBehaviour
{
    public FireSimulator fireSimulator;
    public TerrainGenerator terrainGen;

    [Header("Setari grafic")]
    public int graphWidth = 200;  // latimea graficului in pixeli
    public int graphHeight = 80;   // inaltimea graficului in pixeli
    public float sampleInterval = 0.5f; // cat de des esantionam datele

    // Date statistici
    private int _totalCells;
    private int _currentBurning;
    private int _totalBurned;
    private float _fireStartTime = -1f;
    private float _elapsedTime = 0f;
    private float _spreadSpeed = 0f;
    private int _prevBurned = 0;
    private float _sampleTimer = 0f;
    private bool _fireStarted = false;

    // Date pentru grafic — lista de valori (% ars in timp)
    private List<float> _burnedHistory = new List<float>();
    private List<float> _burningHistory = new List<float>();
    private Texture2D _graphTex;

    // Vegetatie dominanta in zona arsa
    private int[] _vegBurnedCount = new int[4]; // Grass, Shrub, Forest, Rock

    void Start()
    {
        if (fireSimulator == null || terrainGen == null) return;

        // Asteptam ca FireSimulator sa se initializeze
        Invoke("InitStats", 2.5f);

        _graphTex = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);
        ClearGraph();
    }

    void InitStats()
    {
        // Folosim dimensiunea din States, nu din vegetationGrid
        if (fireSimulator.States != null)
        {
            int res = fireSimulator.States.GetLength(0);
            _totalCells = res * res;
            Debug.Log("[FireStatistics] Total celule: " + _totalCells + " | Res: " + res);
        }
    }
    void Update()
    {
        if (fireSimulator == null || fireSimulator.States == null) return;

        // Detectam cand a pornit focul
        if (!_fireStarted)
        {
            int res = fireSimulator.States.GetLength(0);
            for (int z = 0; z < res && !_fireStarted; z++)
                for (int x = 0; x < res && !_fireStarted; x++)
                    if (fireSimulator.States[z, x] == CellState.Burning)
                    {
                        _fireStarted = true;
                        _fireStartTime = Time.time;
                    }
        }

        if (!_fireStarted) return;

        _elapsedTime = Time.time - _fireStartTime;

        // Esantionam la interval
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= sampleInterval)
        {
            _sampleTimer = 0f;
            UpdateStats();
            UpdateGraph();
        }
    }

    void UpdateStats()
    {
        int res = fireSimulator.States.GetLength(0);
        _currentBurning = 0;
        _totalBurned = 0;

        // Resetam contoarele de vegetatie
        for (int i = 0; i < 4; i++) _vegBurnedCount[i] = 0;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                CellState state = fireSimulator.States[z, x];
                if (state == CellState.Burning) _currentBurning++;
                if (state == CellState.Burned || state == CellState.Burning)
                {
                    _totalBurned++;
                    // Contorizam vegetatia arsa
                    if (terrainGen.vegetationGrid != null)
                    {
                        int vegIdx = (int)terrainGen.vegetationGrid[z, x];
                        if (vegIdx < 4) _vegBurnedCount[vegIdx]++;
                    }
                }
            }
        }

        // Viteza de propagare = celule noi arse / interval
        _spreadSpeed = (_totalBurned - _prevBurned) / sampleInterval;
        _prevBurned = _totalBurned;

        // Adaugam la istoric pentru grafic
        float pctBurned = _totalCells > 0 ? (float)_totalBurned / _totalCells : 0f;
        float pctBurning = _totalCells > 0 ? (float)_currentBurning / _totalCells : 0f;

        _burnedHistory.Add(pctBurned);
        _burningHistory.Add(pctBurning);

        // Limitam istoricul la graphWidth puncte
        if (_burnedHistory.Count > graphWidth)
        {
            _burnedHistory.RemoveAt(0);
            _burningHistory.RemoveAt(0);
        }
    }

    void UpdateGraph()
    {
        ClearGraph();

        int count = _burnedHistory.Count;
        if (count < 2) return;

        for (int x = 0; x < graphWidth; x++)
        {
            // Indexul in istoric corespunzator acestui pixel
            int idx = Mathf.RoundToInt((float)x / graphWidth * (count - 1));
            idx = Mathf.Clamp(idx, 0, count - 1);

            // Linia rosie = % total ars
            int burnedY = Mathf.RoundToInt(_burnedHistory[idx] * graphHeight);
            // Linia portocalie = % care arde acum
            int burningY = Mathf.RoundToInt(_burningHistory[idx] * graphHeight * 5f); // amplificat x5 ca sa fie vizibil
            burningY = Mathf.Clamp(burningY, 0, graphHeight - 1);

            // Desenam coloana pentru zona arsa (rosu inchis)
            for (int y = 0; y <= burnedY && y < graphHeight; y++)
                _graphTex.SetPixel(x, y, new Color(0.5f, 0.1f, 0.1f, 0.8f));

            // Linie portocalie pentru zona care arde
            if (burningY >= 0 && burningY < graphHeight)
                for (int y = Mathf.Max(0, burningY - 1); y <= Mathf.Min(graphHeight - 1, burningY + 1); y++)
                    _graphTex.SetPixel(x, y, new Color(1f, 0.5f, 0.1f, 1f));
        }

        // Linie de referinta la 50%
        int halfY = graphHeight / 2;
        for (int x = 0; x < graphWidth; x++)
            if (_graphTex.GetPixel(x, halfY).a < 0.5f)
                _graphTex.SetPixel(x, halfY, new Color(0.4f, 0.4f, 0.4f, 0.5f));

        _graphTex.Apply();
    }

    void ClearGraph()
    {
        Color bg = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        for (int y = 0; y < graphHeight; y++)
            for (int x = 0; x < graphWidth; x++)
                _graphTex.SetPixel(x, y, bg);
        _graphTex.Apply();
    }

    void OnGUI()
    {
        if (!_fireStarted) return;

        float panelW = 230f;
        float panelH = 320f;
        float px = Screen.width - panelW - 10f;
        float py = 10f;

        // Fundal panou
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float x = px + 10f;
        float y = py + 10f;
        float cw = panelW - 20f;

        // Stiluri
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 13;
        titleStyle.normal.textColor = Color.white;

        GUIStyle lblStyle = new GUIStyle(GUI.skin.label);
        lblStyle.fontSize = 12;
        lblStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        GUIStyle valStyle = new GUIStyle(GUI.skin.label);
        valStyle.fontSize = 12;
        valStyle.fontStyle = FontStyle.Bold;
        valStyle.alignment = TextAnchor.MiddleRight;

        GUI.Label(new Rect(x, y, cw, 22f), "Statistici Incendiu", titleStyle);
        y += 26f;

        // Linie separator
        GUI.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, cw, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y += 8f;

        float pctBurned = _totalCells > 0 ? (float)_totalBurned / _totalCells * 100f : 0f;
        float pctBurning = _totalCells > 0 ? (float)_currentBurning / _totalCells * 100f : 0f;

        DrawStat(x, y, cw, "Timp scurs:",
                 FormatTime(_elapsedTime), Color.white, lblStyle, valStyle);
        y += 22f;

        DrawStat(x, y, cw, "Arde acum:",
                 _currentBurning + " celule (" + pctBurning.ToString("F1") + "%)",
                 new Color(1f, 0.5f, 0.1f), lblStyle, valStyle);
        y += 22f;

        DrawStat(x, y, cw, "Total ars:",
                 _totalBurned + " celule (" + pctBurned.ToString("F1") + "%)",
                 new Color(0.8f, 0.2f, 0.2f), lblStyle, valStyle);
        y += 22f;

        DrawStat(x, y, cw, "Viteza propagare:",
                 _spreadSpeed.ToString("F1") + " cel/s",
                 new Color(1f, 0.8f, 0.2f), lblStyle, valStyle);
        y += 22f;

        // Parametrii vant
        string windDir = GetWindDirection();
        DrawStat(x, y, cw, "Vant:",
                 windDir + " (" + (fireSimulator.windStrength * 100f).ToString("F0") + "%)",
                 Color.cyan, lblStyle, valStyle);
        y += 22f;

        // Vegetatie cel mai arsa
        string dominantVeg = GetDominantBurnedVeg();
        DrawStat(x, y, cw, "Vegetatie arsa:", dominantVeg,
                 new Color(0.4f, 0.8f, 0.2f), lblStyle, valStyle);
        y += 26f;

        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, cw, 12f), Texture2D.whiteTexture);
        GUI.color = new Color(0.7f, 0.15f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, cw * (pctBurned / 100f), 12f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y += 18f;

        GUIStyle smallLbl = new GUIStyle(GUI.skin.label);
        smallLbl.fontSize = 10;
        smallLbl.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        GUI.Label(new Rect(x, y, cw, 16f), "Propagare in timp:", lblStyle);
        y += 18f;

        // Desenam graficul
        if (_graphTex != null)
            GUI.DrawTexture(new Rect(x, y, graphWidth, graphHeight), _graphTex);

        // Legenda grafic
        GUI.color = new Color(0.5f, 0.1f, 0.1f);
        GUI.DrawTexture(new Rect(x, y + graphHeight + 4f, 12f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 14f, y + graphHeight, 80f, 16f), "Total ars", smallLbl);

        GUI.color = new Color(1f, 0.5f, 0.1f);
        GUI.DrawTexture(new Rect(x + 90f, y + graphHeight + 4f, 12f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 104f, y + graphHeight, 80f, 16f), "Activ", smallLbl);
    }

    void DrawStat(float x, float y, float w, string label, string value,
                  Color valueColor, GUIStyle lblStyle, GUIStyle valStyle)
    {
        GUI.Label(new Rect(x, y, w * 0.6f, 20f), label, lblStyle);
        valStyle.normal.textColor = valueColor;
        GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 20f), value, valStyle);
    }

    string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return m > 0 ? m + "m " + s + "s" : s + "s";
    }

    string GetWindDirection()
    {
        if (fireSimulator == null) return "—";
        float wx = fireSimulator.windX;
        float wz = fireSimulator.windZ;

        if (Mathf.Abs(wx) < 0.1f && Mathf.Abs(wz) < 0.1f) return "Fara vant";
        if (wz > 0.7f && Mathf.Abs(wx) < 0.4f) return "Nord";
        if (wz < -0.7f && Mathf.Abs(wx) < 0.4f) return "Sud";
        if (wx > 0.7f && Mathf.Abs(wz) < 0.4f) return "Est";
        if (wx < -0.7f && Mathf.Abs(wz) < 0.4f) return "Vest";
        if (wx > 0f && wz > 0f) return "Nord-Est";
        if (wx < 0f && wz > 0f) return "Nord-Vest";
        if (wx > 0f && wz < 0f) return "Sud-Est";
        return "Sud-Vest";
    }

    string GetDominantBurnedVeg()
    {
        string[] names = { "Iarba", "Tufis", "Padure", "Stanca" };
        int maxIdx = 0;
        for (int i = 1; i < 4; i++)
            if (_vegBurnedCount[i] > _vegBurnedCount[maxIdx]) maxIdx = i;

        return _vegBurnedCount[maxIdx] > 0 ? names[maxIdx] : "—";
    }
}