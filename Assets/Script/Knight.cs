using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Knight : MonoBehaviour,IDamageable
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
    [HideInInspector] public bool IsAttacking { get;private set; }
    [HideInInspector] public bool IsCrouching { get; private set; }
    [HideInInspector] public bool IsStun { get;private set; }
    #endregion

    #region Time param
    public float WallHangingTime { get; private set; }
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    #endregion

    bool isJumpCut;
    bool isJumpFalling;
    bool wallJump;

    float wallJumpStartTime;

    #region Input parameter
    Vector2 moveInput;

    public float LastPressedJumpTime { get; private set; }
    public float LastPressedRollTime { get; private set; }
    public float LastPressedAttackTime { get; private set; }
    #endregion

    #region Attack Param
    [SerializeField] KnightAttackData[] AttackDatas;
    float CurrentHeath;
    int currentAttackIndex;
    Collider2D currentAttackCollider;
    float lastAttackTime;
    float currentDamage;
    string currentAttackAnimation;
    #endregion

    #region Check parameter
    [Header("Check")]
    [SerializeField] Transform groundCheckPoint;
    [SerializeField] Vector2 groundCheckSize;
    [Space(5)]
    [SerializeField] Transform wallCheckPoint;
    [SerializeField] Vector2 wallCheckSize;
    #endregion

    [Space(5)]
    #region Layer and Tag
    [SerializeField] LayerMask GroudLayer;
    [SerializeField] LayerMask enemyLayer;
    ContactFilter2D enemyContactFilter;
    #endregion

    Animator animator;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyContactFilter = new ContactFilter2D();
        enemyContactFilter.SetLayerMask(enemyLayer);
    }
    private void Start()
    {
        SetGravityScale(Data.GravityScale);
        IsFacingRight = true;
        CurrentHeath = Data.Heath;
        lastAttackTime = Time.time;
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
        if (Rb.velocity.y <= 0 && IsJumping && !IsSliding)
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
            animator.SetTrigger("Roll");
            StartCoroutine(Roll(direction));
        }
        #endregion

        #region Attack Check
        if(LastPressedAttackTime > 0 && CanAttack())
        {
            IsAttacking = true;
            Attack(); 
        }
        if (IsAttacking)
        {
            Rb.velocity = Vector2.zero;
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
        animator.SetBool("Run", IsRuning);
        animator.SetBool("Jump", IsJumping);
        if (!IsAttacking)
        {
            animator.SetBool("Fall", IsFalling);
        }
        animator.SetBool("WallSlide", IsSliding);
        animator.SetBool("WallJump", IsWallJumping);
        animator.SetBool("Crouch", IsCrouching);
        #endregion
    }
    private void FixedUpdate()
    {
        if (!IsRolling && !IsAttacking && !IsStun)
        {
            Debug.Log("runing");
            if (IsWallJumping)
                Run(Data.WallJumpRunLerp,1);
            else if (IsCrouching)
                Run(1,Data.CrouchRunLerp);
            else
                Run(1,1);
        }
        if (wallJump)
        {
            
            wallJump = false;
        }
        if (IsSliding) Slide();
    }
    #region Control method
    void Run(float LerpAmout,float ratioSpeed)
    {
        
        float targetSpeed = moveInput.x * Data.RunMaxSpeed;
        targetSpeed *= ratioSpeed;
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
    void Attack()
    {
        // thuật toán tính xem sẽ ra đòn nào
        float timeSinceLastAttack = Time.time - lastAttackTime;
        // nếu thời gian từ khi kết thúc đòn đánh và đòn đánh tiếp theo trong tg quy định thì 
        // sẽ gọi đòn tiếp theo trong combo
        // nếu thời gian nghỉ giữa 2 đòn đánh quá lâu hoặc hết combo thì nó bắt đầu lại từ đầu
        if (timeSinceLastAttack <= Data.AttackTimeCombo)
        {
            currentAttackIndex++;
            if (currentAttackIndex >= AttackDatas.Length)
            {
                currentAttackIndex = 0;
            }
        }
        else
        {
            currentAttackIndex = 0;
        }
        currentAttackCollider = AttackDatas[currentAttackIndex].AttackCollider;
        currentAttackAnimation = AttackDatas[currentAttackIndex].AttackParamTransition;
        currentDamage = AttackDatas[currentAttackIndex].RatioDamage * Data.Damage;
        animator.SetTrigger(currentAttackAnimation);
    }
    #endregion

    #region các hàm được gọi trong animation events
    void OnAttackingAnimation()
    {
        List<Collider2D> hits = new List<Collider2D>();
        int hitsLength = Physics2D.OverlapCollider(currentAttackCollider, enemyContactFilter, hits);

        if (hitsLength > 0 )
        {
            
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject.CompareTag("Enemy"))
                {
                    hit.GetComponent<IDamageable>().TakeDame(currentDamage);
                    
                }
            }
        }
    }
    void OnEndAttackAnimation()
    {
        IsAttacking = false;
        lastAttackTime = Time.time;
    }
    void OnEndStunAnimation()
    {
        IsStun = false;
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
        return LastOnGroundTime > 0 && !IsJumping && !IsAttacking && !IsRolling && !IsStun;
    }
    bool CanJumpCut()
    {
        return Rb.velocity.y > 0 && IsJumping && !IsStun;
    }
    bool CanWallJump()
    {
        return LastOnWallTime > 0 && !IsWallJumping && LastOnGroundTime < 0 && !IsStun;
    }
    bool CanWallJumCut()
    {
        return Rb.velocity.y > 0 && IsWallJumping && !IsStun;
    }
    bool CanSlide()
    {
        return LastOnGroundTime < 0 && LastOnWallTime > 0 && !IsStun;
    }
    bool CanRoll()
    {
        return LastOnGroundTime > 0 && !IsRolling && !IsAttacking && !IsStun;
    }

    bool CanAttack()
    {
        return !IsAttacking && !IsSliding && !IsRolling && !IsStun;
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
        IsStun = true;
        IsAttacking = false;
        animator.SetTrigger("Hit");
        CurrentHeath -= damage;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.DrawCube(wallCheckPoint.position, wallCheckSize);
    }
}
