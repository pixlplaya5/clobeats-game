using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinObj : MonoBehaviour
{
    public enum SpinDirection
    {
        X,
        Y,
        Z
    }
    public SpinDirection spinDirection = SpinDirection.Y;
    public float speed = 10f;
    float spinX;
    float spinY;
    float spinZ;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (spinDirection == SpinDirection.X)
        {
            spinX = speed * Time.deltaTime;
        }
        else if (spinDirection == SpinDirection.Y)
        {
            spinY = speed * Time.deltaTime;
        }
        else if (spinDirection == SpinDirection.Z)
        {
            spinZ = speed * Time.deltaTime;
        }
        
        transform.Rotate(new Vector3(spinX, spinY, spinZ));
    }
}
