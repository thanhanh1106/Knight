using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Knight Move Data")]
public class KnightMoveData : ScriptableObject
{
    [Header("Gravity")]
    [HideInInspector] public float GravityStrength;
    [HideInInspector] public float GravityScale;
    [Space(5)]
    public float FallGravityMultiplier;
    public float MaxFallSpeed;
    [Space(5)]
    public float MaxFallGravityMultiplier;
    public float MaxFastFallSpeed;
    [Space(5)]
    public float AttackFallGravityMuliplier;
    public float AttackMaxFallSpeed;
    [Space(20)]

    [Header("Run")]
    public float RunMaxSpeed;
    public float RunAcceleration;
    [HideInInspector] public float RunAccelerationAmount;
    public float RunDecceleration;
    [HideInInspector] public float RunDeccelerationAmount;
    [Space(5)]
    [Range(0f, 1f)] public float RunAccelerationInAirBorne;
    [Range(0f, 1f)] public float RunDeccelerationInAirBorne;
    [Range(0f, 1f)] public float RunExhaustedSpeed;
    [Space(5)]
    public bool DoConseverMomentum = true;

    [Space(20)]

    [Header("Jump")]
    public float JumpHeight;
    public float JumpTimeToApex;
    [HideInInspector] public float JumpForce;
    public float JumpLossStamina;

    [Header("BothJump")]
    public float JumpCutGravityMultiplier;
    [Range(0f, 1f)] public float JumpHangGraviyMultiplier;
    public float JumpHangTimeThreshold;
    [Space(5)]
    public float JumHangAccelerationMultiplier;
    public float JumHangMaxSpeedMultiplier;

    [Space(20)]

    [Header("WallJump")]
    public Vector2 WallJumpForce;
    [Space(5)]
    [Range(0f, 1f)] public float WallJumpRunLerp;
    [Range(0f, 1f)] public float WallJumpTime;
    public bool DoTurnOnWallJump;
    public float WallJumpLossStamina;

    [Space(20)]
    [Header("Slide")]
    public float SlideSpeed;
    public float SlideAcceleration;

    [Space(20)]
    [Header("Roll")]
    [SerializeField] float RollTime;
    [HideInInspector] public float RollStartTime;
    [HideInInspector] public float RollMiddleTime;
    [HideInInspector] public float RollEndTime;
    [SerializeField] float RollSpeed;
    [HideInInspector] public float RollStartSpeed;
    [HideInInspector] public float RollMiddleSpeed;
    [HideInInspector] public float RollEndSpeed;
    public float RollLossStamina;

    [Space(20)]
    [Header("Attack")]
    public float Damage;
    public float Heath;
    public float Stamina;
    public float AttackTimeCombo;
    public float RestoreStaminaBufferTime;
    public float RestoreStaminaSpeed;

    [Space(20)]
    [Header("Crouch")]
    [Range(0f, 1f)] public float CrouchRunLerp;

    [Space(20)]
    [Header("Assists")]
    [Range(0.01f, 0.5f)] public float CoyoteTime;
    [Range(0.01f, 0.5f)] public float JumpInputBufferTime;
    [Range(0.01f, 0.5f)] public float RollInputBufferTime;
    [Range(0.01f, 0.5f)] public float AttackInputBufferTime;



    private void OnValidate()
    {
        //Tính cường độ trọng lực bằng công thức(gravity = 2 * jumpHeight / timeToJumpApex ^ 2)
        GravityStrength = -(2 * JumpHeight) / Mathf.Pow(JumpTimeToApex, 2);
        //Tính toán tỷ lệ gravity scale của Rigidbody(tức là độ mạnh của trọng lực so với giá trị trọng lực mặc định của Unity,
        //project settings/Physics2D).
        GravityScale = GravityStrength / Physics2D.gravity.y;

        //Tính toán lực gia tốc và lực giảm tốc khi chạy bằng công thức:
        //amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed.
        RunAccelerationAmount = (50 * RunAcceleration) / RunMaxSpeed;
        RunDeccelerationAmount = (50 * RunDecceleration) / RunMaxSpeed;

        // Tính toán jumpForce bằng cách sử dụng công thức (initialJumpVelocity = gravity * timeToJumpApex).
        JumpForce = Mathf.Abs(GravityStrength) * JumpTimeToApex;

        RunAcceleration = Mathf.Clamp(RunAcceleration, 0.01f, RunMaxSpeed);
        RunDecceleration = Mathf.Clamp(RunDecceleration, 0.01f, RunMaxSpeed);

        RollStartTime = RollTime * 0.2f;
        RollMiddleTime = RollTime * 0.5f;
        RollEndTime = RollTime * 0.3f;

        RollStartSpeed = RollSpeed * 0.8f;
        RollMiddleSpeed = RollSpeed;
        RollEndSpeed = RollSpeed * 0.6f;
    }
}
