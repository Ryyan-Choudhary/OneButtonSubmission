using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Bootstrap
{
    /// Animated start menu and prologue before the first level loads. Z / right trigger advances.
    /// Uses a dedicated title camera so the gameplay camera is never touched.
    public class TitleFlow : MonoBehaviour
    {
        const string PrologueText =
            "James Bond forgot his gun for the mission, can you imagine that ? " +
            "Being his legendary gun, you decided you should go to him yourself.";

        enum Phase { Menu, Prologue }

        GameBootstrap bootstrap;
        TitleScene scene;
        Camera gameCam;
        Camera titleCam;
        Vector3 titleCamBasePos;
        Quaternion titleCamBaseRot;

        Phase phase = Phase.Menu;
        float phaseStart;
        float flash;
        float fade;
        bool transitioning;

        InputAction confirmAction;
        Texture2D vignetteTex;
        Texture2D whiteTex;

        Color menuSkyBottom = Palette.SkyBottom;
        Color menuSkyTop = Palette.SkyTop;
        Color menuAmbient = Palette.Ambient;
        Color prologueSkyBottom = new Color(0.12f, 0.06f, 0.14f);
        Color prologueSkyTop = new Color(0.42f, 0.18f, 0.28f);
        Color prologueAmbient = new Color(0.14f, 0.10f, 0.16f);

        public static TitleFlow Create(GameBootstrap boot)
        {
            var go = new GameObject("TitleFlow");
            var flow = go.AddComponent<TitleFlow>();
            flow.bootstrap = boot;
            return flow;
        }

        void Awake()
        {
            phaseStart = Time.unscaledTime;
            vignetteTex = MakeVignette(256);
            whiteTex = MakeSolid(4, Color.white);

            confirmAction = new InputAction("Confirm", InputActionType.Button);
            confirmAction.AddBinding("<Keyboard>/z");
            confirmAction.AddBinding("<Gamepad>/rightTrigger");
            confirmAction.performed += OnConfirm;
            confirmAction.Enable();
        }

        void Start()
        {
            gameCam = bootstrap.TitleCamera;

            // Overlay camera for title 3D — gameplay camera stays put for sky/atmosphere.
            titleCam = new GameObject("TitleCamera").AddComponent<Camera>();
            titleCam.CopyFrom(gameCam);
            titleCam.clearFlags = CameraClearFlags.Depth;
            titleCam.depth = gameCam.depth + 1;
            titleCam.transform.SetParent(transform, false);

            titleCamBasePos = new Vector3(0f, 3.8f, -17f);
            titleCamBaseRot = Quaternion.Euler(10f, 0f, 0f);
            titleCam.transform.position = titleCamBasePos;
            titleCam.transform.rotation = titleCamBaseRot;

            scene = TitleScene.Build(titleCam, bootstrap.backgroundSeed, transform);
        }

        void OnDestroy()
        {
            if (confirmAction == null) return;
            confirmAction.performed -= OnConfirm;
            confirmAction.Disable();
            confirmAction.Dispose();
            if (vignetteTex != null) Destroy(vignetteTex);
            if (whiteTex != null) Destroy(whiteTex);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            AnimateTitleCamera(t);
            AnimateAtmosphere(t);
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.8f);
        }

        void AnimateTitleCamera(float t)
        {
            if (titleCam == null) return;
            titleCam.transform.position = titleCamBasePos + new Vector3(
                Mathf.Sin(t * 0.38f) * 0.45f,
                Mathf.Sin(t * 0.52f) * 0.2f,
                0f);
            titleCam.transform.rotation = titleCamBaseRot * Quaternion.Euler(
                Mathf.Sin(t * 0.31f) * 1.8f,
                Mathf.Sin(t * 0.27f) * 2.4f,
                Mathf.Sin(t * 0.41f) * 0.8f);
        }

        void AnimateAtmosphere(float t)
        {
            float menuBlend = phase == Phase.Menu ? 1f : 0f;
            float enter = Mathf.Clamp01((t - phaseStart) / 1.2f);
            if (phase == Phase.Prologue)
                menuBlend = 1f - enter;

            Color skyB = Color.Lerp(prologueSkyBottom, menuSkyBottom, menuBlend);
            Color skyT = Color.Lerp(prologueSkyTop, menuSkyTop, menuBlend);
            Color amb = Color.Lerp(prologueAmbient, menuAmbient, menuBlend);

            if (phase == Phase.Menu)
            {
                float breathe = 0.5f + 0.5f * Mathf.Sin(t * 0.6f);
                skyT = Color.Lerp(skyT, Palette.Summit * 0.35f + skyT * 0.65f, breathe * 0.08f);
            }

            bootstrap.ApplyAtmosphere(amb, skyB, skyT);
            if (titleCam != null)
                titleCam.backgroundColor = skyB;
        }

        void OnConfirm(InputAction.CallbackContext ctx)
        {
            if (transitioning) return;

            if (phase == Phase.Menu)
            {
                phase = Phase.Prologue;
                phaseStart = Time.unscaledTime;
                flash = 1f;
                scene?.SetGunOffset(new Vector3(2.8f, 0f, 0f));
                titleCamBasePos += new Vector3(1.2f, 0f, 0f);
            }
            else
                StartCoroutine(StartGameRoutine());
        }

        IEnumerator StartGameRoutine()
        {
            transitioning = true;
            float t = 0f;
            while (t < 0.7f)
            {
                fade = t / 0.7f;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            fade = 1f;
            bootstrap.BeginGame();
            Destroy(gameObject);
        }

        void OnGUI()
        {
            DrawScanlines();
            if (phase == Phase.Menu)
                DrawMenu();
            else
                DrawPrologue();

            DrawVignette();
            DrawFlash();
            if (fade > 0f) DrawFade();
        }

        void DrawMenu()
        {
            float t = Time.unscaledTime;
            float elapsed = t - phaseStart;
            float pad = Screen.width * 0.10f;

            // title stack — anchored from top, each line gets its own row
            float top = Y(0.05f);
            top = DrawRow("ONE BUTTON", elapsed, 0f, 34, Palette.Summit, top, pad, 40f);
            top += 10f;
            DrawRow("SUBMISSION", elapsed, 0.12f, 26, Palette.Ammo, top, pad, 32f);

            // action stack — anchored from bottom so nothing collides with the gun
            float playIn = EaseOut(Mathf.Clamp01((elapsed - 0.35f) / 0.5f));
            float pulse = 1f + 0.04f * Mathf.Sin(t * 4.5f);
            int playSize = Mathf.RoundToInt((40f + pulse * 6f) * playIn);
            Color playCol = Color.Lerp(Palette.Summit, Palette.Muzzle, 0.5f + 0.5f * Mathf.Sin(t * 3f));
            playCol.a = playIn;
            DrawBottomRow("PLAY", playSize, playCol, Y(0.26f), pad, 52f);

            float promptAlpha = EaseOut(Mathf.Clamp01((elapsed - 0.7f) / 0.4f));
            float bounce = Mathf.Abs(Mathf.Sin(t * 5f)) * 3f;
            Color promptCol = Palette.Ammo;
            promptCol.a = promptAlpha * (0.65f + 0.35f * Mathf.Sin(t * 6f));
            DrawBottomRow("►  press Z", 20, promptCol, Y(0.17f) + bounce, pad, 28f);

            DrawTagline(elapsed, pad);
        }

        void DrawTagline(float elapsed, float pad)
        {
            float a = EaseOut(Mathf.Clamp01((elapsed - 1f) / 0.6f)) * 0.7f;
            if (a <= 0f) return;
            var col = new Color(0.85f, 0.82f, 0.95f, a);
            DrawBottomRow("you ARE the gun", 16, col, Y(0.09f), pad, 24f);
        }

        float DrawRow(string text, float elapsed, float delay, int size, Color color,
            float top, float pad, float rowHeight)
        {
            float p = EaseOut(Mathf.Clamp01((elapsed - delay) / 0.55f));
            float slide = (1f - p) * 20f;
            color.a = p;
            DrawLabel(text, size, color, pad, top + slide, Screen.width - pad * 2f,
                rowHeight, TextAnchor.UpperCenter);
            return top + slide + rowHeight;
        }

        void DrawBottomRow(string text, int size, Color color, float bottom, float pad, float rowHeight)
        {
            float y = Screen.height - bottom - rowHeight;
            DrawLabel(text, size, color, pad, y, Screen.width - pad * 2f,
                rowHeight, TextAnchor.MiddleCenter);
        }

        void DrawPrologue()
        {
            float elapsed = Time.unscaledTime - phaseStart;
            float typeSeconds = PrologueText.Length * 0.028f + 0.5f;
            float reveal = Mathf.Clamp01(elapsed / typeSeconds);
            int chars = Mathf.RoundToInt(reveal * PrologueText.Length);
            string shown = PrologueText.Substring(0, chars);

            if (chars < PrologueText.Length && Mathf.FloorToInt(Time.unscaledTime * 3f) % 2 == 0)
                shown += "▌";

            float boxIn = EaseOut(Mathf.Clamp01(elapsed / 0.5f));
            float pad = Screen.width * 0.12f;
            float boxW = Screen.width - pad * 2f;
            float textTop = Y(0.18f);
            float textHeight = Y(0.40f);
            DrawPrologueFrame(textTop, textHeight, boxIn);

            var textCol = Color.Lerp(new Color(1f, 0.92f, 0.82f, 0f), Color.white, boxIn);
            DrawParagraph(shown, 22, textCol, pad, textTop + 18f, boxW, textHeight);

            if (chars >= PrologueText.Length)
            {
                float promptIn = EaseOut(Mathf.Clamp01((elapsed - typeSeconds - 0.3f) / 0.4f));
                Color c = Palette.Ammo;
                c.a = promptIn * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f));
                float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.5f)) * 4f;
                DrawCentered("►  press Z", 22, c, Y(0.88f) + bounce, 32f);
            }
        }

        void DrawPrologueFrame(float top, float height, float alpha)
        {
            if (alpha <= 0f) return;
            float pad = Screen.width * 0.10f;
            var rect = new Rect(pad, top - 16f, Screen.width - pad * 2f, height + 32f);
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.35f * alpha);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(Palette.Summit.r, Palette.Summit.g, Palette.Summit.b, 0.25f * alpha);
            GUI.Box(new Rect(rect.x, rect.y, rect.width, 3f), GUIContent.none);
            GUI.color = prev;
        }

        void DrawScanlines()
        {
            var prev = GUI.color;
            for (int y = 0; y < Screen.height; y += 4)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.03f);
                GUI.DrawTexture(new Rect(0, y, Screen.width, 1), whiteTex);
            }
            GUI.color = prev;
        }

        void DrawVignette()
        {
            if (vignetteTex == null) return;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTex);
            GUI.color = prev;
        }

        void DrawFlash()
        {
            if (flash <= 0f) return;
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.55f, flash * 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = prev;
        }

        void DrawFade()
        {
            var prev = GUI.color;
            GUI.color = new Color(0.05f, 0.04f, 0.08f, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = prev;
        }

        static float Y(float fraction) => Screen.height * fraction;

        static void DrawLabel(string text, int fontSize, Color color,
            float x, float y, float width, float height, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = anchor,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(x, y, width, height), text, style);
        }

        static void DrawCentered(string text, int fontSize, Color color, float y, float height)
        {
            float pad = Screen.width * 0.10f;
            DrawLabel(text, fontSize, color, pad, y, Screen.width - pad * 2f, height, TextAnchor.MiddleCenter);
        }

        static void DrawParagraph(string text, int fontSize, Color color,
            float x, float y, float width, float height)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(x, y, width, height), text, style);
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        static Texture2D MakeSolid(int size, Color c)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeVignette(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f, cy = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((d - 0.35f) / 0.65f);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a * 0.55f));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
