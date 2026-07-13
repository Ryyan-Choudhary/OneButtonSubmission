using System.Collections;
using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The final-level victory chain. When the boss dies: the cage bursts
    /// open, Bond leaps off the helicopter in chunky arc-steps down to
    /// wherever the gun is, grabs it (his signature kneel-and-fire), and the
    /// ending screen rolls the kill tally before blacking out to the title.
    public class Level10Finale : MonoBehaviour
    {
        public SummitAgent bond;
        public GameObject cage;
        public Transform levelRoot;
        public System.Action onVictory;

        bool triggered;

        public void BossDown()
        {
            if (triggered) return;
            triggered = true;
            StartCoroutine(Rescue());
        }

        IEnumerator Rescue()
        {
            // freeze the fight where it stands — nothing can kill anyone now
            var gunBody = Object.FindFirstObjectByType<GunBody>();
            if (gunBody != null)
            {
                var gc = gunBody.GetComponent<GunController>();
                if (gc != null) gc.enabled = false;
                gunBody.GetComponent<Rigidbody>().isKinematic = true;
                var laser = gunBody.GetComponentInChildren<AimLaser>();
                if (laser != null) laser.gameObject.SetActive(false);
            }
            yield return new WaitForSeconds(0.5f);

            // the cage tears open
            if (cage != null)
            {
                Vector3 cpos = cage.transform.position;
                AudioManager.Play(AudioManager.Sfx.WindowBreak);
                GlassBurst.Spawn(cpos + Vector3.up * 3f, Vector3.left,
                    (int)(cpos.x * 13f), new Color(0.55f, 0.55f, 0.60f),
                    new Color(0.20f, 0.20f, 0.24f), 16);
                Destroy(cage);
            }
            yield return new WaitForSeconds(0.4f);

            if (bond == null || gunBody == null)
            {
                onVictory?.Invoke();
                yield break;
            }

            // Bond leaps off the helicopter toward the gun, stepped arc
            bond.transform.SetParent(levelRoot, true);
            Vector3 from = bond.transform.position;
            Vector3 to = gunBody.transform.position;
            to += new Vector3(to.x < from.x ? 1.9f : -1.9f, -0.2f, 0f); // land beside it

            // face the gun for the landing and the grab
            bond.facing = to.x < from.x ? -1f : 1f;
            bond.transform.localScale = new Vector3(bond.facing * bond.scale, bond.scale, bond.scale);

            const int steps = 10;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * 4.5f; // the leap's arc
                bond.transform.position = p;
                yield return new WaitForSeconds(0.11f);
            }

            // the legend takes his gun back — kneel, two shots, roll credits
            bond.StartCutscene(gunBody, () => onVictory?.Invoke());
        }
    }

    /// End card: mission complete, the final kill tally, then a slow black
    /// fade back to the title. Runs on unscaled time; input-free.
    public class EndingScreen : MonoBehaviour
    {
        const float FadeIn = 1.2f, Hold = 4.6f, FadeOut = 1.5f;

        System.Action onDone;
        float age;
        Texture2D black;
        bool finished;

        public static void Show(System.Action onDone)
        {
            var go = new GameObject("EndingScreen");
            go.AddComponent<EndingScreen>().onDone = onDone;
        }

        void Awake()
        {
            black = new Texture2D(1, 1);
            black.SetPixel(0, 0, Color.black);
            black.Apply();
            AudioManager.Play(AudioManager.Sfx.Victory);
        }

        void OnDestroy()
        {
            if (black != null) Destroy(black);
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (finished || age < FadeIn + Hold + FadeOut) return;
            finished = true;
            var done = onDone;
            Destroy(gameObject);
            done?.Invoke();
        }

        void OnGUI()
        {
            float dark = age < FadeIn ? Mathf.Lerp(0f, 0.8f, age / FadeIn)
                       : age < FadeIn + Hold ? 0.8f
                       : Mathf.Lerp(0.8f, 1f, (age - FadeIn - Hold) / FadeOut);
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, dark);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);

            GUI.color = Color.white;

            // text rides ON TOP of the blackout in bright gold/white, only
            // dipping out in the very last instant before the title returns
            float textIn = Mathf.Clamp01((age - FadeIn * 0.5f) / 0.8f);
            float textOut = 1f - Mathf.Clamp01((age - FadeIn - Hold - FadeOut + 0.35f) / 0.35f);
            float a = textIn * textOut;
            if (a > 0f)
            {
                int big = Mathf.Max(52, Mathf.RoundToInt(Screen.height * 0.07f));
                int mid = Mathf.Max(34, Mathf.RoundToInt(Screen.height * 0.042f));
                int small = Mathf.Max(24, Mathf.RoundToInt(Screen.height * 0.028f));

                Label("MISSION COMPLETE", big, new Color(1f, 0.85f, 0.25f, a),
                    Screen.height * 0.32f);
                Label($"FINAL KILLS   {GameStats.Kills}", mid, new Color(1f, 1f, 1f, a),
                    Screen.height * 0.47f);
                Label("the legend has his gun back", small, new Color(1f, 0.95f, 0.75f, 0.85f * a),
                    Screen.height * 0.58f);
            }
            GUI.color = prev;
        }

        void Label(string text, int size, Color color, float y)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(0, y, Screen.width, size * 1.4f), text, style);
        }
    }
}
