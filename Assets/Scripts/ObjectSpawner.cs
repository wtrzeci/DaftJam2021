using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Elympics;

public class ObjectSpawner : ElympicsMonoBehaviour, IUpdatable
{
    [SerializeField] float TimerIntervals = 2f;
    float Timer;
    [SerializeField] string [] SpawnableObjects;
    [SerializeField] float ObjectXVelocity = 10;
    // Start is called before the first frame update
    void Start()
    {
        Timer = TimerIntervals;
        
    }

    // Update is called once per frame
    void IUpdatable.ElympicsUpdate()
    {
        SpawnObstacle();
    }
    private void SpawnObstacle()
    {
        Timer -= Time.deltaTime;
        if (Timer <= 0)
        {
            int index = Random.Range(0, SpawnableObjects.Length);
            var newBall = ElympicsInstantiate(SpawnableObjects[index]);
            newBall.GetComponent<Rigidbody2D>().velocity = new Vector2(ObjectXVelocity, 0);
            Timer = TimerIntervals;
        }
    }

}
