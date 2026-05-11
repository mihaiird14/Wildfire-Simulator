# Wildfire Simulator

Simulator de incendii de padure dezvoltat in **Unity 6** cu generare procedurala a terenului, simulare prin **Cellular Automata**, AI pentru animale si efecte vizuale realiste.

---

## Descriere

Proiectul simuleaza propagarea unui incendiu de padure in functie de mai multi factori reali:
- **Vant** (directie + intensitate)
- **Tipul de vegetatie** (iarba, tufisuri, padure, stanca)
- **Inclinatia terenului** (focul urca mai repede decat coboara)
- **Umiditate** (ploaia incetineste propagarea)

---

## Controale

| Actiune | Control |
|---|---|
| Pornire foc | Click stanga pe harta 2D |
| Schimba modul camera | Tab |
| Miscare camera la sol | WASD |
| Urcare / Coborare | E / Q |
| Rotire camera | Click dreapta + mouse |
| Sprint | Shift + WASD |

---

## Moduri de vizualizare

### Modul 1 — Harta 2D (default)
Camera ortografica de sus. Gridul colorat pe tipuri de vegetatie. Focul se vede propagandu-se in timp real.

| Culoare | Semnificatie |
|---|---|
| Verde deschis | Grass (iarba) |
| Maro | Shrub (tufisuri) |
| Verde inchis | Forest (padure) |
| Gri | Rock (stanca) |
| Portocaliu | Celula care arde |
| Negru | Celula arsa |

### Modul 2 — Vedere 3D la sol
Camera perspectiva cu particule de foc + fum + scantei, animale cu AI, post-processing.

---

## Parametrii simulare

La pornire apare un popup de configurare:

- **Directia vantului** — grid 3x3 (N, S, E, V, NE, NV, SE, SV, fara vant)
- **Intensitatea vantului** — Slab / Mediu / Puternic
- **Umiditate** — slider 0% (uscat) pana la 100% (ploaie)

---

## Tipuri de vegetatie

| Tip | Aprindere | Durata ardere | Viteza spread |
|---|---|---|---|
| Grass | 85% | 4 sec | 1.8x |
| Shrub | 65% | 10 sec | 1.3x |
| Forest | 35% | 25 sec | 0.9x |
| Rock | 0% | Nu arde | 0x |

---

## Formula de propagare

```
P = ignitionChance x spreadMultiplier x windFactor x slopeFactor x moistureFactor x simSpeed
```

- **windFactor** = `1 + windStrength x cos(unghi)` — max +90% in directia vantului
- **slopeFactor** = `1 + heightDiff x 5` — urcare pana la 2.5x, coborare pana la 0.3x
- **moistureFactor** = `1 - moisture x 0.8` — ploaie reduce probabilitatea cu 80%

---

## Animale — Decision Tree AI

5 tipuri de animale cu comportament bazat pe arbore de decizii:

```
dist < criticalRadius  → FUGA activa (viteza maxima + zig-zag)
dist < detectionRadius → EVITARE preventiva
stamina = 0            → ODIHNA
altfel                 → RATACIRE aleatoare pe NavMesh
```

| Animal | Detectie | Viteza fuga | Zig-zag |
|---|---|---|---|
| Cerb | 25 u | 10 u/s | 10% |
| Mistret | 10 u | 7 u/s | 80% |
| Iepure | 15 u | 14 u/s | 90% |
| Lup | 30 u | 11 u/s | 5% |
| Vulpe | 20 u | 9 u/s | 40% |

Daca un animal este prins de foc → moare (culoare rosie, cazut pe o parte).


---

## Tehnologii

- **Unity 6** — motor de joc
- **Universal Render Pipeline (URP)** — randare
- **NavMesh AI Navigation** — navigatie animale
- **Perlin Noise** — generare procedurala teren
- **Cellular Automata** — simulare propagare foc
- **Particle System** — efecte vizuale foc/fum/ploaie
- **Post-Processing** — Bloom, Tonemapping ACES, Vignette

---

## Disciplina

Proiect realizat pentru **Programarea Aplicatiilor de Simulare** — acopera:
- Generare Procedurala de Continut (capitolul 6, 7)
- Inteligenta Artificiala in Jocuri (capitolul 8)
- Transformari si Randare (capitolul 3)
- Introducere in Simulare (capitolul 1)
