using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class BallKiller : ElympicsMonoBehaviour
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
        if (collision.name is "Model")
        {

            collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value -=1;
            Debug.Log(collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value -= 1);
            if (collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value is 0)
            {
                collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>().GameOver();
            }
            ElympicsDestroy(gameObject);
        }
    }
}
