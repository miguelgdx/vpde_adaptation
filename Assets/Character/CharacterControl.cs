using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.HeroEditor.Common.CharacterScripts;

public class CharacterControl : MonoBehaviour, OnEnemyHit.Effect
{
    public float speed = 12f;        // Mov. speed
    public float accel = 50f;
    public bool onGround = false;   // Character touching ground
    public float fuerzaSalto = 200.0f;
    public bool drawRayForces = false;
    public bool drawRayGround = true;

    Rigidbody2D rb;
    private Collider2D col;
    private float groundRayVerticalOffset = 0.5f;   // Offset of ray (so it's not strictly the base)
    private float groundRayLength = 5f;             // Length of the ray from the collider base to the ground.
    private Animator animator;
    private bool mayusPressed = false;
    private float runningSpeed = 0f;
    private float CASTING_PUSHBACK_FORCE = 100f;
    private Collider2D selfCollider;
    private float lastEnemyHitTime;
    public HealthControlUI hCUI;

    CharacterDefinitions defScript;
    CharacterDefinitions.CharacterStats stats;
    private Character heroCharacterScript;
    private float mH;
    private GameObject gameController;
    private float _lastJumpTime = 0;
    private float JUMP_INTERVAL = 0.1f;
    private float _timeOnGround = 0f;

    // Mobile Device.
    public Joystick mobileMoveJoystick;
    public JumpButtonVirtual mobileJumpButton;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = this.GetComponentInChildren<Animator>();
        runningSpeed = speed + 4f;
        selfCollider = GetComponent<Collider2D>();
        applyLayerMaskToChildren();
        // Set up hit delegate
        OnEnemyHit oehScript = GetComponent<OnEnemyHit>();
        oehScript.executionDelegate = OnEnemyHitEffect;
        // Definitions
        defScript = GetComponent<CharacterDefinitions>();
        stats = defScript.stats;
        heroCharacterScript = GetComponent<Character>();
        gameController = GameObject.FindGameObjectWithTag("GameController");
    }

    // Update is called once per frame
    private void Update()
    {
        mH = 0;

        // Walk/Run control
        if (Input.GetKey(KeyCode.D) || mobileMoveJoystick.Horizontal > 0)
            mH = 1.0f;
        if (Input.GetKey(KeyCode.A) || mobileMoveJoystick.Horizontal < 0)
            mH = -1.0f;
        if (Input.GetKey(KeyCode.LeftShift) || isAndroid() && Mathf.Abs(mobileMoveJoystick.Horizontal) > 0.5)
            mayusPressed = true;
        if (Input.GetKeyUp(KeyCode.LeftShift) || isAndroid() && Mathf.Abs(mobileMoveJoystick.Horizontal) < 0.5)
            mayusPressed = false;

        // Jump control
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || mobileJumpButton.isPressed()) && isGrounded() && canJump())
        {
            _lastJumpTime = Time.time;
            // Reset any vertical force on the character
            rb.velocity = new Vector2(rb.velocity.x, 0);
            Vector2 vSalto = new Vector3(0, 1);
            // Apply force to make it jump
            rb.AddForce(vSalto * fuerzaSalto, ForceMode2D.Force);
        }
    }
    void FixedUpdate()
    {
        // Animation control.
        switch (stats.status)
        {
            case CharacterDefinitions.CharacterStats.StatusTypes.injured:
                // Forze facial expression till status is over
                heroCharacterScript.SetExpression("Injured");
                animator.Play("Hit", 0);
                checkAnimationLastFrame();
                break;
            case CharacterDefinitions.CharacterStats.StatusTypes.dead:
                //heroCharacterScript.SetExpression("Dead");
                animator.Play("DieBack", 0);
                checkAnimationLastFrame();
                return;
        }

        onGround = isGrounded();
        if (onGround)
            _timeOnGround += Time.deltaTime;
        else
            _timeOnGround = 0f;
        

        Vector2 currentHSpeed = new Vector2(rb.velocity.x, 0f);

        // Get acceleration only on ground and after pressing a key.
        if (Mathf.Abs(mH) >= 1.0f && isGrounded())
        {
            if (mayusPressed)
            {
                resetAnimationStatus();
                animator.SetBool("Run", true);
            }
            else
            {
                resetAnimationStatus();
                animator.SetBool("Walk", true);
            }
                
            // Target speed to be walking or running speed.
            Vector2 targetSpeed = new Vector2(speed, 0f);
            if (mayusPressed)
                targetSpeed.x = runningSpeed;
            targetSpeed = targetSpeed * mH;

            // Accelerate = Vf - V0
            rb.AddForce(targetSpeed - currentHSpeed, ForceMode2D.Force);
            //rb.velocity = new Vector2(mH * speed, rb.velocity.y);
        }

        // If on air, player wants to move, allow the character to move just a little to the sides
        if (Mathf.Abs(mH) >= 1.0f && !isGrounded())
        {
            Vector2 targetSpeed = new Vector2(speed, 0f);
            if (mayusPressed)
                targetSpeed.x = runningSpeed;
            targetSpeed = targetSpeed * mH;
            rb.AddForce((targetSpeed - currentHSpeed) * 0.5f, ForceMode2D.Force);
        }

            if (Mathf.Abs(rb.velocity.x) < 0.01f && isGrounded())
        {
            setIdleAnimation();
        }

        if (drawRayForces)
        {
            Debug.DrawRay(transform.position, currentHSpeed, Color.blue);
            Debug.DrawRay(transform.position, rb.velocity, Color.red);
        }

        // Control jump animation by detecting vertical speed.
        // This way we get animation when falling and jumping.
        if (Mathf.Abs(rb.velocity.y) >= 1f)
        {
            setJumpAnimation();
        }

        // If ground ray says we're not on ground but we're not falling, move towards ground.
        if (!isGrounded() && Mathf.Abs(rb.velocity.y) <= 0.05f)
        {
            Vector2 skidSpeed = new Vector2(1.0f, 0);
            Vector2 maxSkidSpeed = new Vector2(20.0f, 0);
            // Check sides and move towards the falling side.
            if (shouldSkidAt(0.5f))
            {
                rb.AddForce(maxSkidSpeed - skidSpeed, ForceMode2D.Force);
            }
            else if (shouldSkidAt(-0.5f))
            {
                skidSpeed *= -1;
                maxSkidSpeed *= -1;
                rb.AddForce(maxSkidSpeed - skidSpeed, ForceMode2D.Force);
            }
        }

        
        
    }

    // Can the character jump?
    private bool canJump()
    {
        bool result = true;
        if (stats.status == CharacterDefinitions.CharacterStats.StatusTypes.dead || (Time.time - _lastJumpTime < JUMP_INTERVAL) || (_timeOnGround < JUMP_INTERVAL))
            result = false;

        return result;

    }

    public bool canAttack()
    {
        bool result = true;
        if (stats.status == CharacterDefinitions.CharacterStats.StatusTypes.dead)
            result = false;

        return result;

    }


    private void checkAnimationLastFrame()
    {
        // If it's "Hit"
        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")){
            // Back to idle.
            setIdleAnimation();
            stats.status = CharacterDefinitions.CharacterStats.StatusTypes.idle;
        }
    }

    private bool shouldSkidAt(float xDist)
    {
        // Distance to the base of the collider from the pivot point (we don't want to use the centre).
        float distToColliderBase = col.bounds.extents.y;
        // Ray start position begin on the feet to make it clear (with a certain offset so it's not completely the base).
        Vector3 rayStartPosition = new Vector3(transform.position.x + xDist, transform.position.y - distToColliderBase + groundRayVerticalOffset, transform.position.z);
        Debug.DrawRay(rayStartPosition, new Vector3(0, -(groundRayLength), 0), Color.cyan);
        RaycastHit2D r = Physics2D.Raycast(rayStartPosition, new Vector3(0, -groundRayLength, 0), groundRayLength);
        
        // We will return true if the distance to the ray is less than the double of its offset.
        if (r.collider == null || r.distance >= (groundRayVerticalOffset * 2))
            return true;
        else
            return false;
    }

    private bool isGrounded()
    {
        // Distance to the base of the collider from the pivot point (we don't want to use the centre).
        float distToColliderBase = col.bounds.extents.y;
        // Ray start position begin on the feet to make it clear (with a certain offset so it's not completely the base).
        Vector3 rayStartPosition = new Vector3(transform.position.x, transform.position.y - distToColliderBase + groundRayVerticalOffset, transform.position.z);
        Debug.DrawRay(rayStartPosition, new Vector3(0, -(groundRayLength), 0), Color.yellow);
        RaycastHit2D[] r = Physics2D.RaycastAll(rayStartPosition, new Vector3(0, -groundRayLength, 0), groundRayLength);
        
        //Debug.Log("Distancia actual de colisión: " + r.distance + " contra: " + r.collider.name);
        // Nothing to raycast?
        if (r.Length == 0)
            return false;
        // Is the other object ground?
        bool otherIsGround = false;
        RaycastHit2D terrainHit = r[0];
        List<RaycastHit2D> candidates = new List<RaycastHit2D>();
        foreach (RaycastHit2D hit in r)
        {
            //Debug.Log("Has collided against: " + hit.collider.gameObject.name);
            CustomCollisionProperties ccp = hit.collider.gameObject.GetComponent<CustomCollisionProperties>();
            //Debug.Log("ccp: " + ccp);
            if (ccp != null && ccp.ObjectType == CustomCollisionProperties.ObjectTypeEnum.Terrain)
            {
                otherIsGround = true;
                candidates.Add(hit);
            }
        }

        // Get first one as default.
        if (candidates.Count != 0)
            terrainHit = candidates[0];

        // Iterate over all candidates and select the nearest
        foreach (RaycastHit2D cand in candidates)
        {
            if (terrainHit.distance > cand.distance)
                terrainHit = cand;
        }

        if (!otherIsGround)
            return false;

        // We will return true if the distance to the ray is less than the double of its offset.
        if (terrainHit.collider != null && terrainHit.distance < (groundRayVerticalOffset * 2) && otherIsGround)
            return true;
        else
            return false;
    }

    private void setIdleAnimation()
    {
        resetAnimationStatus();
        animator.SetBool("Idle", true);
    }

    private void setJumpAnimation()
    {
        resetAnimationStatus();
        animator.SetBool("Jump", true);
    }

    private void resetAnimationStatus()
    {
        animator.SetBool("Run", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Idle", false);
        animator.SetBool("Jump", false);
    }

    public void onCastingFinished()
    {
        
    }

    public void castingPushback(Vector2 target)
    {
        // Move in the opposite direction of target
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 dir = target - currentPos;

        Vector2 forceVector = (dir.normalized * -1) * CASTING_PUSHBACK_FORCE;
        Debug.DrawRay(transform.position, forceVector, Color.cyan);
        rb.AddForce(forceVector, ForceMode2D.Force);
    }

    public bool isLookingLeft()
    {
        return transform.localScale.x < 0;
    }

    public void applyLayerMaskToChildren()
    {
        SpriteRenderer[] Sprites = GetComponentsInChildren<SpriteRenderer>();
        for (var i = 0; i < Sprites.Length; i++)
        {
            Sprites[i].sortingLayerName = "Character";
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }

    public void OnEnemyHitEffect(OnEnemyHit.HitDetails details)
    {
        // Prevent multiple hits at the same time.
        if (Time.time - lastEnemyHitTime > stats.TIME_BETWEEN_ENEMY_HIT && stats.status != CharacterDefinitions.CharacterStats.StatusTypes.dead)
        {
            // Play hit sound.
            GameObject globalSound = (GameObject)Instantiate(Resources.Load("GlobalSoundSource"));
            GlobalSoundPlay soundScript = globalSound.GetComponent<GlobalSoundPlay>();
            soundScript.playGlobalSound(details.hitSfx, 0.4f);

            lastEnemyHitTime = Time.time;
            hCUI.consumeHp(20);
            if (hCUI.currentHp <= 0)
            {
                resetAnimationStatus();
                stats.status = CharacterDefinitions.CharacterStats.StatusTypes.dead;
                animator.SetBool("DieBack", true);
                GlobalEvents geScript = gameController.GetComponent<GlobalEvents>();
                geScript.showGameOverScreen();
            }
            else
            {
                stats.status = CharacterDefinitions.CharacterStats.StatusTypes.injured;
                Invoke("hitAnimationToNormal", stats.TIME_WITH_HIT_EXPRESSION);
            }
        }
        
    }
    private void hitAnimationToNormal()
    {
        if (heroCharacterScript.Expression.Equals("Injured")){
            heroCharacterScript.SetExpression("Default");
        }
    }

    public float getMovement()
    {
        return mH;
    }

    public bool isAndroid()
    {
        return (Application.platform == RuntimePlatform.Android);
    }
}
