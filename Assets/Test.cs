using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    private Vector3 next = new Vector3(1, 1, 1);
    void FixedUpdate()
    {
        GetComponent<Rigidbody>().AddForce(Vector3.forward);
        GetComponent<Rigidbody>().MovePosition(next);
        Debug.Log("0 : " + transform.position);
        GetComponent<Rigidbody>().AddForce(-Vector3.forward);
        
        next += Vector3.forward;
        GetComponent<Rigidbody>().MovePosition(next);
        Debug.Log("1 : " + transform.position);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
