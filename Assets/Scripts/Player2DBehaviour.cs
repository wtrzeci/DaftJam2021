using UnityEngine;
using Elympics;

public class Player2DBehaviour : ElympicsMonoBehaviour, IUpdatable
{
	private readonly int _movingAnimatorBool = Animator.StringToHash("IsWalking");

	[SerializeField] private Animator  characterAnimator  = null;
	[SerializeField] private float     speed              = 5;
    [SerializeField] private float     hp                 = 3;
    [SerializeField] private float     jumpSpeed          = 10;
	[SerializeField] private float     force              = 100;
	[SerializeField] private float     fireDuration       = 0.4f;
	[SerializeField] private string    ballPrefabName     = "BlueBall";
	[SerializeField] private Transform ballAnchor        = null;
    [SerializeField] public  int       associatedPlayerId = ElympicsPlayer.INVALID_ID;

	// using ElympicsFloats for timer allows you to predict their change, allowing for ball spawn prediction
	// it's not neccessary, but without this prediction the spawned balls might appear and then disappear and appear again because of lags
	// in general, the more predictable a behaviour is the less jitter there will be in laggy network conditions
	private readonly ElympicsFloat _timerForFiring = new ElympicsFloat();

	private Vector3     _cachedVelocity;
	private Rigidbody2D _rigidbody;

	private ElympicsBool _hasJumped = new ElympicsBool();

	private bool IsFiring => _timerForFiring > 0;

	private bool IsGrounded => Physics2D.Raycast(transform.position, Vector2.down, 0.2f);

	private void ApplyMovement(float horizontalAxis)
	{
		var direction = Vector2.right * horizontalAxis;
		var velocity = direction * speed;
		_rigidbody.velocity = new Vector2(velocity.x, _rigidbody.velocity.y);
		//ApplyRotation(horizontalAxis);
	}

	private void ApplyRotation(float movementDirection)
	{
		if (movementDirection == 0)
			return;
		transform.localRotation = Quaternion.Euler(0, movementDirection < 0 ? 0 : 180, 0);
	}

	private void ApplyJump()
	{
		_rigidbody.velocity += Vector2.up * jumpSpeed;
		_hasJumped.Value = true;
	}

	private void ApplyLanding()
	{
		_hasJumped.Value = false;
	}

	public void Jump()
	{
        if(!_hasJumped)
			ApplyJump();
	}

	public void Move(float horizontalAxis)
	{
		ApplyMovement(horizontalAxis);
	}

	public void Fire()
	{
		if (IsFiring)
			return;
		SpawnBall();
		_timerForFiring.Value = fireDuration;
	}

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody2D>();
	}

	private void SpawnBall()
	{
        // using ElympicsInstantiate to synchronize instantiating a prefab
        // the instantiated prefab has to be in Resources with this name

        var newBall = ElympicsInstantiate(ballPrefabName);


        if (this.gameObject.name == "Player1")
        {

            newBall.transform.position = ballAnchor.transform.position;
            newBall.transform.rotation = ballAnchor.transform.rotation;
            newBall.GetComponent<Rigidbody2D>().AddForce((-newBall.transform.right/2 + newBall.transform.up) * force);

            var newBall2 = ElympicsInstantiate(ballPrefabName);

            newBall2.transform.position = ballAnchor.transform.position;
            newBall2.transform.rotation = ballAnchor.transform.rotation;
            newBall2.GetComponent<Rigidbody2D>().AddForce((newBall2.transform.right/2 + newBall2.transform.up) * force);
            
        }
        else if (this.gameObject.name == "Player2")
        {

            newBall.transform.position = ballAnchor.transform.position;
            newBall.transform.rotation = ballAnchor.transform.rotation;
            newBall.GetComponent<Rigidbody2D>().AddForce((-newBall.transform.up) * force);

        }

    }

	// this fixed update is called during prediction loop, you can use it to predict changes to ElympicsVars, 
	// for example, timers or health lost when standing in lava
	public void ElympicsUpdate()
	{
		if (_timerForFiring > 0)
			DecreaseFiringTimer();

		if (_rigidbody.velocity.y <= 0 && _hasJumped && IsGrounded)
			ApplyLanding();
	}

	private void DecreaseFiringTimer()
	{
		_timerForFiring.Value -= Time.deltaTime;
	}
    
    private void OnCollisionEnter2D(Collision2D collision)
    {

        //ZDERZENIE Z KROWĄ/KARABELĄ

        if (this.gameObject.name == "Player2")
        {

            if (collision.gameObject.name == "BallGreen(Clone)")
            {

                //Destroy(collision.collider.gameObject);
                this.hp--;
                Debug.Log(this.gameObject.name + " hp: " + hp);
                ElympicsDestroy(collision.collider.gameObject);

            }

        }
        else if (this.gameObject.name == "Player1")
        {

            if (collision.gameObject.name == "BallPurple(Clone)")
            {

                //Destroy(collision.collider.gameObject);
                this.hp--;
                Debug.Log(this.gameObject.name + " hp: " + hp);
                ElympicsDestroy(collision.collider.gameObject);
                
            }

        }


    }

}
