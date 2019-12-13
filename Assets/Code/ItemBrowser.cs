using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBrowser : MonoBehaviour {
    [SerializeField] ItemHolder[] allItems;
    
    int itemIndex;
    int totalItems;

    int currentItemVariantIndex;

    void Initialise() {
        itemIndex = 0;
        currentItemVariantIndex = 0;
        totalItems = allItems.Length;
        allItems[itemIndex].gameObject.SetActive(true);
        ShowItemVariant(0);
    }

    void Start() {
        Initialise();
    }

    public void ShowNextItem() {
        allItems[itemIndex].ResetVariantsVisibility();
        if (++itemIndex == totalItems)
            itemIndex = 0;
        ShowItemVariant(0);
    }

    public void ShowPrevItem() {
        allItems[itemIndex].ResetVariantsVisibility();
        if (--itemIndex < 0)
            itemIndex = totalItems - 1;
        ShowItemVariant(0);
    }

    public void ShowItemVariant(int variantIndex) {
        allItems[itemIndex].ItemVariants[currentItemVariantIndex].SetActive(false);
        currentItemVariantIndex = variantIndex;
        allItems[itemIndex].ItemVariants[currentItemVariantIndex].SetActive(true);
    }
    
}
