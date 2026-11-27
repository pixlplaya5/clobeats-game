using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PostProcCheck : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        PostProcessLayer postProcLayer = GetComponent<PostProcessLayer>();
        if (GameManager.Instance)
        {
            if (GameManager.Instance.GetPostProcSetting())
            {
                postProcLayer.enabled = true;
            }
            else
            {
                postProcLayer.enabled = false;
            }
        }
    }
}
