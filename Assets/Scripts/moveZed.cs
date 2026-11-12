using UnityEngine;

public class moveZed : MonoBehaviour
{
    // unused, use GlobalMoveY instead
    public bool tiled = true;
    public bool isPlaying = true;
 
    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            transform.Translate(0, -GameManager.Instance.GetNoteSpeed() * Time.deltaTime, 0);
            if (tiled && transform.position.y < 0f) // highway texture
            {
                transform.position = new Vector3(transform.position.x, 20f, transform.position.z);
            }
        }
        else
        {
            // Do nothing when not playing
        }
    }
}
