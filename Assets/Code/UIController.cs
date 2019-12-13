using UnityEngine;
using UnityEngine.UI;

namespace Code.Editor {
    public class UIController : MonoBehaviour {
        [SerializeField] ItemBrowser itemBrowser;

        [SerializeField] Button nextButton;
        [SerializeField] Button prevButton;
        [SerializeField] Button origButton;
        [SerializeField] Button compressedButton;
        [SerializeField] Button compressedScaledButton;

        void Initialise() {
            nextButton.onClick.AddListener(itemBrowser.ShowNextItem);
            prevButton.onClick.AddListener(itemBrowser.ShowPrevItem);
        }

        void Start() {
            Initialise();
        }
    }
}