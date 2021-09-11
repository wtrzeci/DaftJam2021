using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class GarbageCollector : ElympicsMonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        ElympicsDestroy(collision.gameObject);
    }
}
