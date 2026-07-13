using UnityEngine;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// A breakable glass pane blocking a route. Solid until shot: the gun
    /// bounces off it, enemy rounds are stopped by it, but one player
    /// bullet (or a missile blast) shatters it — spending a shell to open
    /// the short way is the demolition trade.
    public class GlassBarrier : MonoBehaviour
    {
        bool broken;

        public void Shatter(Vector3 dir)
        {
            if (broken) return;
            broken = true;
            AudioManager.Play(AudioManager.Sfx.WindowBreak);
            Vector3 pos = transform.position;
            GlassBurst.Spawn(pos, dir, (int)(pos.x * 31f + pos.y * 7f));
            Destroy(gameObject);
        }
    }
}
