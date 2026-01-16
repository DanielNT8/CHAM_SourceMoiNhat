namespace Outsource.Modules.AI.Scripts
{
    using UnityEngine;
    using UnityEngine.UI;

    public class ButtonAI : MonoBehaviour
    {
        [field: SerializeField] public Button    Button    { get; set; }
        [field: SerializeField] public Image     IconNoti  { get; set; }
        [field: SerializeField] public IssueType IssueType { get; private set; }

        private void Start()
        {
            this.IconNoti.gameObject.SetActive(false);
        }
    }
}