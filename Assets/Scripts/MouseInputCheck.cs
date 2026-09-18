using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInputCheck : MonoBehaviour
{
    public bool allowMouseInput = false;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        Rewired.Integration.UnityUI.RewiredStandaloneInputModule inputModule = FindFirstObjectByType<Rewired.Integration.UnityUI.RewiredStandaloneInputModule>();
        inputModule.allowMouseInput = allowMouseInput;
    }
}
