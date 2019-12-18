using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInputController : MonoBehaviour {
    [SerializeField] ItemBrowser itemBrowser;

    float horizontalInput;
    float verticalInput;
    
    
    void Update() {
        GetInput();
        Rotate();
    }

    void GetInput() {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    void Rotate() {
        itemBrowser.ItemVariantTransform.Rotate(new Vector3(-verticalInput, horizontalInput,0));
    }
}

