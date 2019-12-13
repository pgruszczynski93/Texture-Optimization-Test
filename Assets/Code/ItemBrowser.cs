using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBrowser : MonoBehaviour {
    [SerializeField] ItemHolder[] allItems;
    
    int itemIndex;
    int totalItems;

    void Initialise() {
        itemIndex = 0;
        totalItems = allItems.Length;
        allItems[itemIndex].gameObject.SetActive(true);
    }

    void Start() {
        Initialise();
    }

    public void ShowNextItem() {

        allItems[itemIndex].gameObject.SetActive(false);
        if (++itemIndex == totalItems)
            itemIndex = 0;
        allItems[itemIndex].gameObject.SetActive(true);
    }

    public void ShowPrevItem() {
        allItems[itemIndex].gameObject.SetActive(false);
        if (--itemIndex < 0)
            itemIndex = totalItems - 1;
        allItems[itemIndex].gameObject.SetActive(true);
    }
    
}
