using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class BallKiller : ElympicsMonoBehaviour
{

    [SerializeField] private string parentName;

    void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.name is "Model" /*&& collision.name.Equals(parentName)*/)
        {

            collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value -=1;
            Debug.Log(collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value -= 1);
            if (collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>()._hp.Value <= 0)
            {
                collision.transform.parent.gameObject.GetComponent<Player2DBehaviour>().GameOver();
            }
            ElympicsDestroy(gameObject);
        }
    }
}
