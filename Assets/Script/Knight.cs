using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : MonoBehaviour
{
    public KnightMoveData Data;
    public Rigidbody2D Rb;

    #region state param
    [HideInInspector] public bool IsDie { get; private set; }
    [HideInInspector] public bool IsFacingRight { get; private set; }
    [HideInInspector] public bool IsJumping { get; set; }
    [HideInInspector] public bool IsWallJumping { get; set; }
    [HideInInspector] public bool IsClinging { get; private set; }
    [HideInInspector] public bool IsClimping { get; private set; }
    [HideInInspector] public bool IsSliding { get; private set; }
    [HideInInspector] public bool IsRuning { get; private set; }
    [HideInInspector] public bool IsIdle { get; private set; }
    [HideInInspector] public bool IsFalling { get; private set; }
    [HideInInspector] public bool IsStillStrength => WallHangingTime <= Data.WallHangingTimeAllowed;
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
    public float LastPressedClingingTime { get; private set; }
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
        LastPressedClingingTime -= Time.deltaTime;
        #endregion

        #region input Handler
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.x != 0)
            TurnAround(moveInput.x);

        if (Input.GetKeyDown(KeyCode.Space))
            OnJumpInput();

        if (Input.GetKeyUp(KeyCode.Space))
            OnJumpUpInput();

        if (Input.GetKey(KeyCode.L)) 
            OnClingInput();
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

        if (IsClinging || IsSliding) IsJumping = false;

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
            wallJump = true;
        }
        #endregion

        #region Slide check
        IsSliding = moveInput.x != 0 && CanSlide();
        #endregion

        #region Cling check
        if (CanCling() && LastPressedClingingTime > 0)
        {
            IsClinging = true;
            Cling();
        }
        else
        {
            IsClinging = false;
        }
        #endregion

        #region Climb check
        if (CanClimb() && moveInput.y != 0)
            IsClimping = true;
        else
            IsClimping = false;
        #endregion

        #region Gravity
        if (IsClinging || IsSliding) SetGravityScale(0);
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
        if (LastOnGroundTime > 0 && moveInput.x != 0 && !IsSliding)
        {
            IsRuning = true;
            IsIdle = false;
        }
        else if (LastOnGroundTime > 0 && moveInput.x == 0 && !IsSliding)
        {
            IsRuning = false;
            IsIdle = true;
        }
        else
        {
            IsRuning = false;
            IsIdle = false;
        }

        if (LastOnGroundTime < 0 && Rb.velocity.y < 0 && !IsSliding && !IsClimping)
        {
            IsFalling = true;
        }
        else IsFalling = false;
        #endregion

        #region Animation Handler
        if (IsRuning)
            animationController.ChangeAnimationState("Run");
        if (IsIdle)
            animationController.ChangeAnimationState("Idle");
        if (IsJumping)
            animationController.ChangeAnimationState("Jump");
        if (IsFalling)
        {
            animationController.ChangeAnimationState("JumFallInBetween");
            Invoke("FallAnimation",0.1f);
        }
            
        if (IsClinging && !IsClimping)
            animationController.ChangeAnimationState("WallHang");
        if (IsSliding)
            animationController.ChangeAnimationState("WallSlide");
        if (IsClinging && IsClimping)
            animationController.ChangeAnimationState("WallClimb");

        #endregion
    }
    private void FixedUpdate()
    {
        if (IsWallJumping)
            Run(Data.WallJumpRunLerp);
        else
            Run(1);
        if (jump)
        {
            
            jump = false;
        }
        if (wallJump)
        {
            WallJump();
            wallJump = false;
        }
        if (IsSliding) Slide();
        if (IsClimping) Climb();
    }
    #region Control method
    void Run(float LerpAmout)
    {
        float targetSpeed = moveInput.x * Data.RunMaxSpeed;
        targetSpeed = Mathf.Lerp(Rb.velocity.x, targetSpeed, LerpAmout);

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

    void Cling()
    {
        if (!IsClimping)
        {
            Rb.velocity = new Vector2(Rb.velocity.x, 0);
        }
        WallHangingTime += Time.deltaTime;
    }

    void Climb()
    {
        float targetSpeed;
        if (moveInput.y > 0)
            targetSpeed = moveInput.y * Data.ClimpUpSpeed;
        else if (moveInput.y < 0)
            targetSpeed = moveInput.y * Data.ClimpDownSpeed;
        else
            targetSpeed = 0;

        float speedDifferent = targetSpeed - Rb.velocity.y;
        float movement = speedDifferent * 1 / Time.fixedDeltaTime;
        Rb.AddForce(movement * Vector2.up, ForceMode2D.Force);
    }

    #endregion

    #region input Call
    public void OnJumpInput()
    {
        // khi nhấn nhảy thì cài lại LastPressedJumpTime > 0 => thỏa mãn 1 điều kiện để nhảy
        LastPressedJumpTime = Data.JumpInputBufferTime;
    }
    public void OnJumpUpInput()
    {
        if (CanJumpCut() || CanWallJumCut()) 
            isJumpCut = true;
    }
    public void OnClingInput()
    {
        LastPressedClingingTime = Data.ClingInputBufferTime;
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
        return LastOnWallTime > 0 && !IsWallJumping && LastOnGroundTime < 0 && IsStillStrength;
    }
    bool CanWallJumCut()
    {
        return Rb.velocity.y > 0 && IsWallJumping;
    }
    bool CanSlide()
    {
        return LastOnGroundTime < 0 && LastOnWallTime > 0 && !IsClinging;
    }
    bool CanCling()
    {
        return LastOnGroundTime <= 0 && LastOnWallTime > 0 && IsStillStrength && !IsWallJumping;
    }
    bool CanClimb()
    {
        return LastOnWallTime > 0 && !IsJumping && IsClinging;
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
    void FallAnimation()
    {
        animationController.ChangeAnimationState("Fall");
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.DrawCube(wallCheckPoint.position, wallCheckSize);
    }

}
