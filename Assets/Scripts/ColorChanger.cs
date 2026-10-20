using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public Color gemColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Renderer rdr = GetComponent<Renderer>();
        rdr.material.color = gemColor;
    }
}
