using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float MOVE_ACCEL = (0.12f * 60.0f);
    public float GROUND_FRICTION = 0.85f;
    public float GRAVITY = (-0.05f * 60.0f);
    public float JUMP_VEL = 0.75f;
    public float JUMP_MIN_TIME = 0.06f;
    public float JUMP_MAX_TIME = 0.20f;
    public float AIR_FALL_FRICTION = 0.975f;
    public float AIR_MOVE_FRICTION = 0.85f;

    private Rigidbody2D m_rigidBody = null;
    private bool m_jumpPressed = false;
    private bool m_jumpHeld = false;
    private bool m_wantsRight = false;
    private bool m_wantsLeft = false;
    private float m_stateTimer = 0.0f;
    private Vector2 m_vel = new Vector2(0, 0);
    private List<GameObject> m_groundObjects = new List<GameObject>();

    //adding effect for movement, touch and fall
    [Header("Effects")]
    public ParticleSystem movementParticle;
    public ParticleSystem fallParticle;
    public ParticleSystem touchParticle;
    public ParticleSystem explosionParticle;
    public float occurAfterVelocity;
    public float dustFormationPeriod;
    public Sprite happyFace;
    public Sprite sadFace;
    public Sprite veryHappyFace;
    public float trampolineBouncy;
    private float m_counter;
    private bool m_jumpTrampoline;
    private SpriteRenderer m_spritePlayer;
    
    private enum PlayerState
    {
        PS_IDLE = 0,
        PS_FALLING,
        PS_JUMPING,
        PS_WALKING
    };

    private PlayerState m_state = PlayerState.PS_IDLE;

    // Use this for initialization
    void Start ()
    {
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
        m_spritePlayer = GetComponent<SpriteRenderer>();
    }

    void Update()
	{
        UpdateInput();
    }
	
    void FixedUpdate()
    {
        switch (m_state)
        {
            case PlayerState.PS_IDLE:
                Idle();
                break;
            case PlayerState.PS_FALLING:
                Falling();
                break;
            case PlayerState.PS_JUMPING:
                Jumping();
                break;
            case PlayerState.PS_WALKING:
                Walking();
                break;
            default:
                break;
        }
    }

    void Idle()
    {
        m_vel = Vector2.zero;
        //Check to see whether to go into movement of some sort
        if (m_groundObjects.Count == 0)
        {
            //No longer on the ground, fall.
            m_state = PlayerState.PS_FALLING;
            return;
        }

        //Check input for other state transitions
        if (m_jumpPressed || m_jumpHeld)
        {
            m_stateTimer = 0;
            m_state = PlayerState.PS_JUMPING;
            return;
        }

        //Test for input to move
        if (m_wantsLeft || m_wantsRight)
        {
            m_state = PlayerState.PS_WALKING;
            return;
        }
    }

    void Falling()
    {
        m_vel.y += GRAVITY * Time.fixedDeltaTime;
        m_vel.y *= AIR_FALL_FRICTION;
        if (m_wantsLeft)
        {
            m_vel.x -= MOVE_ACCEL * Time.fixedDeltaTime;
            m_spritePlayer.flipX = true;
        }
        else if (m_wantsRight)
        {
            m_vel.x += MOVE_ACCEL * Time.fixedDeltaTime;
            m_spritePlayer.flipX = false;
        }

        m_vel.x *= AIR_MOVE_FRICTION;

        ApplyVelocity();
    }

    void Jumping()
    {
        m_stateTimer += Time.fixedDeltaTime;

        if (m_stateTimer < JUMP_MIN_TIME || (m_jumpHeld && m_stateTimer < JUMP_MAX_TIME))
        {
            m_vel.y = JUMP_VEL;
        }
        m_vel.y += (GRAVITY + trampolineBouncy) * Time.fixedDeltaTime;

        if (m_vel.y <= 0)
        {
            m_state = PlayerState.PS_FALLING;
        }

        if (m_wantsLeft)
        {
            m_vel.x -= MOVE_ACCEL * Time.fixedDeltaTime;
            m_spritePlayer.flipX = true;
        }
        else if (m_wantsRight)
        {
            m_vel.x += MOVE_ACCEL * Time.fixedDeltaTime;
            m_spritePlayer.flipX = false;
        }

        m_vel.x *= AIR_MOVE_FRICTION;

        ApplyVelocity();
    }

    void Walking()
    {
        if (m_wantsLeft)
        {
            m_vel.x -= MOVE_ACCEL * Time.fixedDeltaTime;
            //flip the sprite
            m_spritePlayer.flipX = true;
        }
        else if (m_wantsRight)
        {
            m_vel.x += MOVE_ACCEL * Time.fixedDeltaTime;
            //flip the sprite
            m_spritePlayer.flipX = false;
        }
        else if (m_vel.x >= -0.05f && m_vel.x <= 0.05)
        {
            m_state = PlayerState.PS_IDLE;
            m_vel.x = 0;
        }

        m_vel.y = 0;
        m_vel.x *= GROUND_FRICTION;

        ApplyVelocity();

        //adding the movement effect
        m_counter += Time.deltaTime;
  
        if (Mathf.Abs(m_vel.x)> occurAfterVelocity)
        {
            if (m_counter > dustFormationPeriod)
            {
                movementParticle.Play();
                m_counter = 0;
            }
        }

        if (m_groundObjects.Count == 0)
        {
            //No longer on the ground, fall.
            m_state = PlayerState.PS_FALLING;
            return;
        }

        if (m_jumpPressed || m_jumpHeld || m_jumpTrampoline)
        {
            m_stateTimer = 0;
            m_state = PlayerState.PS_JUMPING;
            return;
        }
    }

    void ApplyVelocity()
    {
        Vector3 pos = m_rigidBody.transform.position;
        pos.x += m_vel.x;
        pos.y += m_vel.y;
        m_rigidBody.transform.position = pos;
    }

    void UpdateInput()
    {
        m_wantsLeft = Input.GetKey(KeyCode.LeftArrow);
        m_wantsRight = Input.GetKey(KeyCode.RightArrow);
        m_jumpPressed = Input.GetKeyDown(KeyCode.UpArrow);
        m_jumpHeld = Input.GetKey(KeyCode.UpArrow);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        m_groundObjects.Remove(collision.gameObject);
    }

    private void ProcessCollision(Collision2D collision)
    {
        m_groundObjects.Remove(collision.gameObject);
        Vector3 pos = m_rigidBody.transform.position;
        
        foreach (ContactPoint2D contact in collision.contacts)
        {
            //Push back out
            Vector2 impulse = contact.normal * (contact.normalImpulse / Time.fixedDeltaTime);
            pos.x += impulse.x;
            pos.y += impulse.y;
            if (Mathf.Abs(contact.normal.y) > Mathf.Abs(contact.normal.x))
            {
                //Hit ground
                if (contact.normal.y > 0)
                {
                    if (m_groundObjects.Contains(contact.collider.gameObject) == false)
                    {
                        m_groundObjects.Add(contact.collider.gameObject);
                    }
                    if (m_state == PlayerState.PS_FALLING)
                    {
                        if (m_jumpTrampoline)
                        {
                            Debug.Log("Jump repeat");
                            m_stateTimer = 0;
                            m_state = PlayerState.PS_JUMPING;
                        }
                        else
                        {
                            //If we've been pushed up, we've hit the ground.  Go to a ground-based state.
                            if (m_wantsRight || m_wantsLeft)
                            {
                                m_state = PlayerState.PS_WALKING;
                            }
                            else
                            {
                                m_state = PlayerState.PS_IDLE;
                            }
                            //adding the effect for hit ground
                            fallParticle.Play();
                            trampolineBouncy = 0;
                            m_spritePlayer.sprite = happyFace;
                        }
                    }
                   
                }
                //Hit Roof
                else
                {
                    m_vel.y = 0;
                    m_state = PlayerState.PS_FALLING;
                    //adding the effect for hit roof
                    touchParticle.Play();
                }
            }
            else
            {
                if ((contact.normal.x > 0 && m_vel.x < 0) || (contact.normal.x < 0 && m_vel.x > 0))
                {
                    m_vel.x = 0;
                }
            }
        }
        m_rigidBody.transform.position = pos;
    }
    public void PlayerInDanger()
    {
        m_spritePlayer.sprite = sadFace;
        if (!explosionParticle.isPlaying)
        {
            explosionParticle.Play();
        }

    }
    public void PlayerExitDanger()
    {
        m_spritePlayer.sprite = happyFace;   
    }
    public void PlayerEnterTrampoline()
    {
        m_jumpTrampoline = true;
        trampolineBouncy += 0.7f;
        m_spritePlayer.sprite = veryHappyFace;
    }
    public void PlayerExitTrampoline()
    {
        m_jumpTrampoline = false;
    }
}
