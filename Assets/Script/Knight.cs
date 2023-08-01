using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : MonoBehaviour
{
    public KnightMoveData Data;
    public Rigidbody2D Rb;

    #region state param
    [HideInInspector] public bool IsDie => CurrentHeath <= 0;
    [HideInInspector] public bool IsFacingRight { get; private set; }
    [HideInInspector] public bool IsJumping { get; set; }
    [HideInInspector] public bool IsWallJumping { get; set; }
    [HideInInspector] public bool IsSliding { get; private set; }
    [HideInInspector] public bool IsRuning { get; private set; }
    [HideInInspector] public bool IsRolling { get; private set; } 
    [HideInInspector] public bool IsIdle { get; private set; }
    [HideInInspector] public bool IsFalling { get; private set; }
    [HideInInspector] public bool IsAttacking { get; private set; }
    [HideInInspector] public bool IsCrouching { get; private set; }
    #endregion

    #region Time param
    public float WallHangingTime { get; private set; }
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    #endregion

    bool isJumpCut;
    bool isJumpFalling;
    bool jump;
    bool wallJump;

    float wallJumpStartTime;

    #region Input parameter
    Vector2 moveInput;

    public float LastPressedJumpTime { get; private set; }
    public float LastPressedRollTime { get; private set; }
    public float LastPressedAttackTime { get; private set; }
    #endregion

    #region Attack Param
    public float Damage;
    public float Heath;
    float CurrentHeath;
    #endregion

    #region Check parameter
    [Header("Check")]
    [SerializeField] Transform groundCheckPoint;
    [SerializeField] Vector2 groundCheckSize;
    [Space(5)]
    [SerializeField] Transform wallCheckPoint;
    [SerializeField] Vector2 wallCheckSize;
    #endregion

    #region Layer and Tag
    [SerializeField] LayerMask GroudLayer;
    #endregion

    AnimationController animationController;
    Animator animator;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animationController = new AnimationController(animator);
    }
    private void Start()
    {
        SetGravityScale(Data.GravityScale);
        IsFacingRight = true;
    }
    private void Update()
    {
        #region Times
        LastOnGroundTime -= Time.deltaTime;
        LastOnWallTime -= Time.deltaTime;

        LastPressedJumpTime -= Time.deltaTime;
        LastPressedRollTime -= Time.deltaTime;
        LastPressedAttackTime -= Time.deltaTime;
        #endregion

        #region input Handler
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.x != 0 && CanTurnArround())
            TurnAround(moveInput.x);

        if (Input.GetKeyDown(KeyCode.Space))
            OnJumpInput();

        if (Input.GetKeyUp(KeyCode.Space))
            OnJumpUpInput();

        if (Input.GetKeyDown(KeyCode.LeftShift))
            OnRollInput();

        if(Input.GetKeyDown(KeyCode.C))
            OnCrouchIput();

        if(Input.GetMouseButtonDown(0))
            OnAttackInput();

        #endregion

        #region Colision Check
        if (!IsJumping && !IsWallJumping)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, GroudLayer))
            {
                LastOnGroundTime = Data.CoyoteTime;
            }
            if (Physics2D.OverlapBox(wallCheckPoint.position, wallCheckSize, 0, GroudLayer))
            {
                LastOnWallTime = Data.CoyoteTime;
            }
        }
        #endregion

        #region Jump Check
        if (Rb.velocity.y <= 0 && IsJumping)
        {
            IsJumping = false;
            isJumpFalling = true;
        }

        if (IsSliding) IsJumping = false;

        if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
            isJumpCut = false;
            isJumpFalling = false;
        }

        if (IsWallJumping && Time.time - wallJumpStartTime > Data.WallJumpTime)
            IsWallJumping = false;

        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            isJumpFalling = false;
            isJumpCut = false;
            IsWallJumping = false;
            IsIdle = false;
            IsRuning = false;
            IsCrouching = false;
            jump = true;
            Jump();
        }
        else if (CanWallJump() && LastPressedJumpTime > 0)
        {
            IsWallJumping = true;
            IsJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            wallJumpStartTime = Time.time;
            WallJump();
        }
        #endregion

        #region Slide check
        IsSliding = moveInput.x != 0 && CanSlide();
        #endregion

        #region Roll check
        if(LastPressedRollTime > 0 && CanRoll())
        {
            Vector2 direction = IsFacingRight ? Vector2.right : Vector2.left;
            IsRolling = true;
            IsJumping = false;
            isJumpCut = false;
            IsWallJumping = false;
            StartCoroutine(Roll(direction));
        }
        #endregion

        #region Attack Check
        if(LastPressedAttackTime > 0 && CanAttack())
        {
            IsAttacking = true;
            StartCoroutine(Attack());

        }
        #endregion

        #region Gravity
        if (IsSliding) SetGravityScale(0);
        else if (IsAttacking && Rb.velocity.y < 0)
        {
            SetGravityScale(Data.AttackFallGravityMuliplier);
            
            Rb.velocity = new Vector2(Rb.velocity.x, Mathf.Max(Rb.velocity.y, -Data.AttackMaxFallSpeed));
        }
        // nếu đang rơi người chơi bấm nút xuống
        else if (Rb.velocity.y < 0 && moveInput.y < 0)
        {
            SetGravityScale(Data.GravityScale * Data.MaxFallGravityMultiplier);
            // giới hạn tốc độ rơi không cho rơi quá nhanh
            // do giá trị là âm nên max thực ra nó là lấy min
            Rb.velocity = new Vector2(Rb.velocity.x, Mathf.Max(Rb.velocity.y, -Data.MaxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            SetGravityScale(Data.GravityScale * Data.JumpCutGravityMultiplier);
            Rb.velocity = new Vector2(Rb.velocity.x, Mathf.Max(Rb.velocity.y, -Data.MaxFallSpeed));
        }
        // nếu nhảy hoặc rơi chưa đạt vận tốc cài sẵn thì thay đổi gravity
        else if ((IsJumping || isJumpFalling || IsWallJumping) && Mathf.Abs(Rb.velocity.y) < Data.JumpHangTimeThreshold)
        {
            SetGravityScale(Data.JumpHangGraviyMultiplier);
        }
        else if (Rb.velocity.y < 0)
        {
            SetGravityScale(Data.GravityScale * Data.FallGravityMultiplier);
            Rb.velocity = new Vector2(Rb.velocity.x, Mathf.Max(Rb.velocity.y, -Data.MaxFallSpeed));
        }
        else
        {
            SetGravityScale(Data.GravityScale);
        }
        #endregion

        #region Check state
        if (LastOnGroundTime > 0 && moveInput.x != 0 && !IsSliding && !IsRolling && !IsAttacking)
        {
            IsRuning = true;
            IsIdle = false;
        }
        else if (LastOnGroundTime > 0 && moveInput.x == 0 && !IsSliding && !IsRolling && !IsAttacking )
        {
            IsRuning = false;
            IsIdle = true;
        }
        else
        {
            IsRuning = false;
            IsIdle = false;
        }

        if (LastOnGroundTime < 0 && Rb.velocity.y < 0 && !IsSliding)
        {
            IsFalling = true;
        }
        else IsFalling = false;
        #endregion

        #region Animation Handler
        if (IsCrouching)
        {
            if (IsAttacking)
                animationController.ChangeAnimationState("CrouchAttack");
            if(IsRuning)
                animationController.ChangeAnimationState("CrouchWalk");
            if(IsIdle)
                animationController.ChangeAnimationState("Crouch");
        }
        else
        {
            if (IsRuning)
                animationController.ChangeAnimationState("Run");

            if (IsIdle)
                animationController.ChangeAnimationState("Idle");

            if ((IsJumping || IsWallJumping) && !IsAttacking)
                animationController.ChangeAnimationState("Jump");

            if (IsFalling && !IsAttacking)
                animationController.ChangeAnimationState("Fall");

            if (IsSliding)
                animationController.ChangeAnimationState("WallSlide");

            if (IsRolling)
                animationController.ChangeAnimationState("Roll");

            if (IsAttacking)
                animationController.ChangeAnimationState("Attack1");
        }

        #endregion
    }
    private void FixedUpdate()
    {
        if (!IsRolling)
        {
            if (IsWallJumping)
                Run(Data.WallJumpRunLerp);
            else if (IsCrouching)
                Run(Data.CrouchRunLerp);
            else
                Run(1);
        }

        if (jump)
        {

            
            jump = false;
        }
        if (wallJump)
        {
            
            wallJump = false;
        }
        if (IsSliding) Slide();
    }
    #region Control method
    void Run(float LerpAmout)
    {
        
        float targetSpeed = moveInput.x * Data.RunMaxSpeed;
        targetSpeed = Mathf.Lerp(Rb.velocity.x, targetSpeed, LerpAmout);
        Debug.Log(targetSpeed + " ABC");

        float acceleration;
        if (LastOnGroundTime > 0)
            acceleration = Mathf.Abs(targetSpeed) > 0.01f ?
                Data.RunAccelerationAmount : Data.RunDeccelerationAmount;
        else
            acceleration = Mathf.Abs(targetSpeed) > 0.01f ?
                Data.RunAccelerationAmount * Data.RunAccelerationInAirBorne :
                Data.RunDeccelerationAmount * Data.RunAccelerationInAirBorne;

        if ((IsJumping || IsWallJumping || isJumpFalling) && Mathf.Abs(Rb.velocity.y) < Data.JumpHangTimeThreshold)
        {
            acceleration *= Data.JumHangAccelerationMultiplier;
            targetSpeed *= Data.JumHangMaxSpeedMultiplier;
        }

        float speedDifferent = targetSpeed - Rb.velocity.x;
        float movemet = speedDifferent * acceleration;
        Rb.AddForce(movemet * Vector2.right, ForceMode2D.Force);
    }

    void Jump()
    {
        LastOnGroundTime = 0;
        LastPressedJumpTime = 0;

        float force = Data.JumpForce;
        if (Rb.velocity.y < 0) force -= Rb.velocity.y;

        Rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    void WallJump()
    {
        LastPressedJumpTime = 0;
        LastOnWallTime = 0;

        // kiến người chơi nhanh hết lực bám hơn khi nhảy tường
        WallHangingTime++;

        int direction = IsFacingRight ? -1 : 1;
        Vector2 force = Data.WallJumpForce;
        force.x *= direction;

        if (Mathf.Sign(Rb.velocity.x) != Mathf.Sign(force.x))
            force.x -= Rb.velocity.x; // trái dấu nên thực ra đây là phép cộng
        if (Rb.velocity.y < 0)
            force.y -= Rb.velocity.y;
        Rb.AddForce(force, ForceMode2D.Impulse);

    }

    void Slide()
    {
        if (Rb.velocity.y > 0)
            Rb.AddForce(Vector2.down * Rb.velocity.y, ForceMode2D.Impulse);

        // nhìn có vẻ ngược logic nhưng bên data để slide Speed là âm thì trượt xuống, dương thì trượt lên
        float speedDifferent = Data.SlideSpeed - Rb.velocity.y;

        float movemet = speedDifferent * 1 / Time.fixedDeltaTime;
        Rb.AddForce(movemet * Vector2.up, ForceMode2D.Force);
    }

    IEnumerator Roll(Vector2 diretion)
    {
        LastPressedRollTime = 0;
        float startTime = Time.time;

        diretion.Normalize();
        while(Time.time - startTime < Data.RollStartTime)
        {
            Rb.velocity = diretion*Data.RollStartSpeed;
            yield return null;
        }
        startTime = Time.time;
        while(Time.time - startTime < Data.RollMiddleTime)
        {
            Rb.velocity = diretion *Data.RollMiddleSpeed;
            yield return null;
        }
        startTime = Time.time;
        while (Time.time - startTime < Data.RollEndTime)
        {
            Rb.velocity = diretion * Data.RollEndSpeed;
            yield return null;
        }

        IsRolling = false;
    }
    IEnumerator Attack()
    {
        LastPressedAttackTime = 0;
        float startTime = Time.time;
        while( Time.time - startTime < 0.4f)
        {
            Rb.velocity = new Vector2(0,Rb.velocity.y);
            yield return null;
        }
        IsAttacking = false;
    }
    #endregion

    #region input Call
    public void OnJumpInput()
    {
        // khi nhấn nhảy thì cài lại LastPressedJumpTime > 0 => thỏa mãn 1 điều kiện để nhảy
        LastPressedJumpTime = Data.JumpInputBufferTime;
    }
    void OnJumpUpInput()
    {
        if (CanJumpCut() || CanWallJumCut()) 
            isJumpCut = true;
    }
    void OnRollInput()
    {
        LastPressedRollTime = Data.RollInputBufferTime;
    }
    void OnAttackInput()
    {
        LastPressedAttackTime = Data.AttackInputBufferTime;
    }
    void OnCrouchIput()
    {
        IsCrouching = !IsCrouching;
    }
    #endregion

    #region Check
    bool CanJump()
    {
        return LastOnGroundTime > 0 && !IsJumping;
    }
    bool CanJumpCut()
    {
        return Rb.velocity.y > 0 && IsJumping;
    }
    bool CanWallJump()
    {
        return LastOnWallTime > 0 && !IsWallJumping && LastOnGroundTime < 0 ;
    }
    bool CanWallJumCut()
    {
        return Rb.velocity.y > 0 && IsWallJumping;
    }
    bool CanSlide()
    {
        return LastOnGroundTime < 0 && LastOnWallTime > 0;
    }
    bool CanRoll()
    {
        return LastOnGroundTime > 0 && !IsRolling;
    }

    bool CanAttack()
    {
        return !IsAttacking && !IsSliding && !IsRolling;
    }
    bool CanTurnArround()
    {
        return !IsAttacking;
    }
    #endregion
    void SetGravityScale(float Scale)
    {
        Rb.gravityScale = Scale;
    }
    void TurnAround(float xDirectionMove)
    {
        float eulerAnglesY = xDirectionMove > 0 ? 0 : 180;
        IsFacingRight = xDirectionMove > 0;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, eulerAnglesY, transform.eulerAngles.z);
    }

    public void TakeDame(float damage)
    {
        CurrentHeath -= damage;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.DrawCube(wallCheckPoint.position, wallCheckSize);
    }

}
