using UnityEngine;

public class ItemHolder : MonoBehaviour {
    [SerializeField] GameObject[] itemVariants;

    public GameObject[] ItemVariants => itemVariants;

    public void ResetVariantsVisibility() {
        for (var i = 0; i < itemVariants.Length; i++) {
            itemVariants[i].SetActive(false);
        }
    }
}
