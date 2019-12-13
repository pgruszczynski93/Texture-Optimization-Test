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
            origButton.onClick.AddListener(() => itemBrowser.ShowItemVariant(0));
            compressedButton.onClick.AddListener(() => itemBrowser.ShowItemVariant(1));
            compressedScaledButton.onClick.AddListener(() => itemBrowser.ShowItemVariant(2));
        }

        void Start() {
            Initialise();
        }
    }
}