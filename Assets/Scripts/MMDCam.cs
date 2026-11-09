using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main camera used for MMD motion.
/// gameObject position/rotation is used as usual, adds distance parameter as camera zoom
/// </summary>
public class MMDCam : MonoBehaviour
{
    public static MMDCam Instance;
    public GameObject targetCamera;
    public float distance = 0f; // z distance (zoom)

    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        targetCamera.transform.position = new Vector3(
        targetCamera.transform.position.x,
        targetCamera.transform.position.y,
        -distance);
    }

    public void SetEffect(string effKey)
    {
        Debug.Log($"[MMDCam.SetEffect({effKey})] This feature is not implemented yet.");
    }

    // these are required if we're using singleton references
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, pos.z); // ensures no conflicts
    }

    public Quaternion GetRotation()
    {
        return transform.rotation;
    }

    public void SetRotation(Vector3 rot)
    {
        transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
    }
    public float GetDistance()
    {
        return distance;
    }
    public void SetDistance(float dis)
    {
        distance = dis;
    }

    public bool GetVisibility()
    {
        return targetCamera.GetComponent<Camera>().enabled;
    }
    public void SetVisibility(bool toggle)
    {
        targetCamera.GetComponent<Camera>().enabled = toggle;
    }
}
