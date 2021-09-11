using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class ObjectCollisions : ElympicsMonoBehaviour
{

    private void OnCollisionEnter2D(Collision2D collision)
    {
                
        if(collision.gameObject.name == "Player1" || collision.gameObject.name == "Player2")
        {

            collision.gameObject.GetComponent<Player2DBehaviour>()._hp.Value--; ;
            Debug.Log(collision.gameObject.name + " hp: " + 
                collision.gameObject.GetComponent<Player2DBehaviour>()._hp.Value);

        }

        ElympicsDestroy(this.gameObject);
        Debug.Log("kolizja");

    }

}
