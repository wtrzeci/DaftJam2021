using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class ObjectCollisions : ElympicsMonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        ElympicsDestroy(this.gameObject);
        Debug.Log("kolizja");
    }
}
