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
    [Space(5)]
    public bool DoConseverMomentum = true;

    [Space(20)]

    [Header("Jump")]
    public float JumpHeight;
    public float JumpTimeToApex;
    [HideInInspector] public float JumpForce;

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

    [Space(20)]

    [Header("Cling")]
    public float WallHangingTimeAllowed;

    [Space(20)]
    [Header("Climp")]
    public float ClimpUpSpeed;
    public float ClimpDownSpeed;

    [Space(20)]
    [Header("Slide")]
    public float SlideSpeed;
    public float SlideAcceleration;

    [Space(20)]
    [Header("Assists")]
    [Range(0.01f, 0.5f)] public float CoyoteTime;
    [Range(0.01f, 0.5f)] public float JumpInputBufferTime;
    [Range(0.01f, 0.5f)] public float ClingInputBufferTime;
    [Range(0.01f, 0.5f)] public float DashInputBufferTime;


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
    }
}
