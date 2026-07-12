using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// The win volume over the final shelf. If an agent is waiting there, the
    /// gun's arrival hands off to his cutscene, which reports the win when he
    /// is done; otherwise the win fires immediately.
    public class SummitTrigger : MonoBehaviour
    {
        public GameManager gameManager;
        public SummitAgent agent;

        bool consumed;

        void OnTriggerEnter(Collider other)
        {
            if (consumed) return;
            var body = other.GetComponentInParent<GunBody>();
            if (body == null) return;
            consumed = true;

            if (agent != null)
                agent.StartCutscene(body, () => gameManager?.TriggerWin());
            else
                gameManager?.TriggerWin();
        }
    }
}
