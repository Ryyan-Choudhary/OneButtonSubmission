using System.Collections;
using UnityEngine;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Components
{
    /// Final-level opening: the camera hangs on the helicopter — Bond in the
    /// cage, the purple suit pacing the deck — while letterboxed lines set
    /// the stakes. Then the camera dives down the whole tower to the entry
    /// window, flash, and the last climb begins. Plays once; retries skip it.
    public class Level10Intro : MonoBehaviour
    {
        public Camera cam;
        public CameraFollow follow;
        public GunController gun;
        public Vector3 heliFocus;
        public float wallRight;
        public float burstHeight;
        public System.Action onLaunch;

        static readonly string[] Lines =
        {
            "They took him.",
            "The man in the purple suit wants the legend gone for good.",
            "Climb. Kill him. Bring Bond home.",
        };
        const float LineSeconds = 2.6f;

        float age;
        float flash;
        int shownLines;
        Texture2D black, white;

        public void Play()
        {
            black = Solid(Color.black);
            white = Solid(Color.white);

            gun.gameObject.SetActive(false); // not on stage yet
            follow.target = null;
            cam.transform.position = heliFocus + new Vector3(-2f, 0.5f, -24f);
            cam.transform.rotation = Quaternion.identity;

            StartCoroutine(Run());
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        IEnumerator Run()
        {
            // hold on the helicopter while the lines land; slow drift closer
            float total = Lines.Length * LineSeconds + 0.8f;
            Vector3 from = cam.transform.position;
            Vector3 to = heliFocus + new Vector3(0f, 0f, -19f);
            float t = 0f;
            while (t < total)
            {
                t += Time.deltaTime;
                shownLines = Mathf.Min(Lines.Length, 1 + (int)(t / LineSeconds));
                cam.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / total));
                yield return null;
            }

            // the dive: whip down the whole tower to the entry window
            Vector3 diveTo = new Vector3(wallRight - 10f, burstHeight + 2f, -20f);
            Vector3 diveFrom = cam.transform.position;
            float d = 0f;
            const float diveSeconds = 1.5f;
            while (d < diveSeconds)
            {
                d += Time.deltaTime;
                float k = d / diveSeconds;
                cam.transform.position = Vector3.Lerp(diveFrom, diveTo, k * k); // accelerating fall
                yield return null;
            }

            flash = 1f;
            gun.gameObject.SetActive(true);
            onLaunch?.Invoke();

            while (flash > 0f)
            {
                flash -= Time.deltaTime * 2.2f;
                yield return null;
            }
            Destroy(gameObject);
        }

        void Update() => age += Time.deltaTime;

        void OnGUI()
        {
            // letterbox bars
            float bar = Screen.height * 0.11f;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, bar), black);
            GUI.DrawTexture(new Rect(0, Screen.height - bar, Screen.width, bar), black);

            // the lines, typewritten one after another in the bottom bar area
            int size = Mathf.Max(28, Mathf.RoundToInt(Screen.height * 0.034f));
            for (int i = 0; i < shownLines; i++)
            {
                float lineAge = age - i * LineSeconds;
                string text = Lines[i];
                int chars = Mathf.Min(text.Length, (int)(lineAge / 0.035f));
                if (chars <= 0) continue;
                bool current = i == shownLines - 1;
                if (!current && i < shownLines - 1) continue; // only the active line stays up

                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = size,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.92f, 0.82f, 0.95f) }
                };
                GUI.Label(new Rect(0, Screen.height - bar * 0.95f, Screen.width, bar * 0.9f),
                    text.Substring(0, chars), style);
            }

            if (flash > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, flash * 0.9f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            }
            GUI.color = prev;
        }

        void OnDestroy()
        {
            if (black != null) Destroy(black);
            if (white != null) Destroy(white);
        }
    }
}
