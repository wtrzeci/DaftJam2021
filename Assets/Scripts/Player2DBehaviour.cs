using UnityEngine;
using Elympics;
using UnityEngine.UI;
using manager = UnityEngine.SceneManagement.SceneManager;

public class Player2DBehaviour : ElympicsMonoBehaviour, IUpdatable
{
	private readonly int _movingAnimatorBool = Animator.StringToHash("IsWalking");

	[SerializeField] private Animator  characterAnimator  = null;
	[SerializeField] private float     speed              = 5;
    [SerializeField] private float     playerHealth       = 5;
    [SerializeField] private float     jumpSpeed          = 10;
	[SerializeField] private float     force              = 100;
    [SerializeField] private float     leftBoundary       = -20f;
    [SerializeField] private float     rightBoundary      = 400f;
    [SerializeField] private float     bottomBoundary     = 0f;
    [SerializeField] private float     upperBoundary      = 0f;
    [SerializeField] private float     fireDuration       = 0.4f;
	[SerializeField] private string    ballPrefabName     = "BlueBall";
	[SerializeField] private Transform ballAnchor        = null;
    [SerializeField] public  int       associatedPlayerId = ElympicsPlayer.INVALID_ID;
    [SerializeField] private Text hpText;

<<<<<<< Updated upstream
=======
    [SerializeField] private AudioClip wroooomSound;
    [SerializeField] private AudioClip shootyshootySound;


>>>>>>> Stashed changes
	// using ElympicsFloats for timer allows you to predict their change, allowing for ball spawn prediction
	// it's not neccessary, but without this prediction the spawned balls might appear and then disappear and appear again because of lags
	// in general, the more predictable a behaviour is the less jitter there will be in laggy network conditions
	private readonly ElympicsFloat _timerForFiring = new ElympicsFloat();

    public ElympicsFloat _hp = new ElympicsFloat();
	private Vector3     _cachedVelocity;
	private Rigidbody2D _rigidbody;

	private ElympicsBool _hasJumped = new ElympicsBool();

	private bool IsFiring => _timerForFiring > 0;

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

	public void Jump()
	{

        if (this.gameObject.name == "Player1")
        {

            if (!_hasJumped)
            {
                _rigidbody.velocity += Vector2.up * jumpSpeed;
                _hasJumped.Value = true;
                this.GetComponent<AudioSource>().clip = wroooomSound;
                this.GetComponent<AudioSource>().Play();

            }

        }
        else
        {
            _rigidbody.velocity += Vector2.up * jumpSpeed;
            _hasJumped.Value = true;
        }

    }

    public void CheckIfJumpPossible()
    {

        if (this.transform.position.y <= this.bottomBoundary)
            _hasJumped.Value = false;

    }

	public void Move(float horizontalAxis)
	{
		ApplyMovement(horizontalAxis);
        PlayerBoundaries();
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
        this._hp.Value = playerHealth;
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
        else 
            if (this.gameObject.name == "Player2")
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

        CheckIfJumpPossible();

	}
    private void Update()
    {
        hpText.text = _hp.Value.ToString();

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
                this._hp.Value--;
                Debug.Log(this.gameObject.name + " hp: " + _hp);
                ElympicsDestroy(collision.collider.gameObject);

            }

        }
        else if (this.gameObject.name == "Player1")
        {

            if (collision.gameObject.name == "BallPurple(Clone)")
            {

                //Destroy(collision.collider.gameObject);
                this._hp.Value--;
                Debug.Log(this.gameObject.name + " hp: " + _hp);
                ElympicsDestroy(collision.collider.gameObject);
                
            }

        }

        if (this._hp.Value <= 0)
            GameOver();

    }

    public void PlayerBoundaries()
    {

        if (this.transform.position.x <= leftBoundary)
            this.transform.position = new Vector3(leftBoundary, this.transform.position.y, this.transform.position.z);
        else if (this.transform.position.x >= rightBoundary)
            this.transform.position = new Vector3(rightBoundary, this.transform.position.y, this.transform.position.z);

        /*
        if (this.transform.position.y < bottomBoundary)
           this.transform.position = new Vector3(this.transform.position.x, bottomBoundary, this.transform.position.z);
        if (this.transform.position.y > upperBoundary)
            this.transform.position = new Vector3(this.transform.position.x, upperBoundary, this.transform.position.z);
        */

    }

    public void GameOver()
    {

        Debug.Log("owo");
        int currentSceneIndex = manager.GetActiveScene().buildIndex;
        manager.LoadScene(currentSceneIndex + 1);
        //PRZEJŚCIE NA ODPOWIEDNIĄ SCENĘ 

    }

}
