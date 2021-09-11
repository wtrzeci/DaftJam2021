using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallKiller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter2D(Collider2D collision)
    {

        Debug.Log("Trigger Detected");
        collision.gameObject.GetComponent<Player2DBehaviour>()._hp.Value -= 1;
        Debug.Log("Collision");
        Debug.Log(collision.gameObject.GetComponent<Player2DBehaviour>()._hp.Value);
    }
}
