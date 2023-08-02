using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Skeleton : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;

    public float MoveSpeed;
    [Space(20)]
    #region Layer mask
    [Header("Layer mask")]
    [SerializeField] LayerMask ground;
    [SerializeField] LayerMask PlayerLayer;
    ContactFilter2D playerContactFillter;
    #endregion
    [Space(20)]
    #region Point
    [SerializeField] Transform groundCheckPoint;
    [SerializeField] Vector2 groundCheckSize;
    [SerializeField] Collider2D weaponCollider;
    [SerializeField] Collider2D PreviousCollider;
    [SerializeField] Collider2D BehindCollider;
    [SerializeField] Transform attackRagePoint;
    [SerializeField] Vector2 attackRageSize;
    List<Collider2D> colliders;
    [SerializeField] Transform[] transformMovePoints;
    #endregion

    [SerializeField] Knight knight;
    Vector2 KnightPosOnGround;

    Vector3[] movementPoint;
    Vector2 groundContact;

    Vector2 currentPoint;
    int currentPointIndex;
    Vector2 currentPositionOnGround;
    Vector2 direction;

    float currentRestingTime;
    float restTime = 1.5f;

    #region Check Param
    bool isOnGround;
    bool isOnFollow;
    bool inAttackRage;
    #endregion

    #region State param
    bool isStandRest;
    bool isWalking;
    bool isAttacking;
    #endregion

    public bool IsDie => currentHeath <= 0;

    [SerializeField] float Heath;
    float currentHeath;
    [SerializeField] float Damage;
    [SerializeField] float AttackingTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerContactFillter = new ContactFilter2D();
        playerContactFillter.SetLayerMask(PlayerLayer);
        colliders = new List<Collider2D>();
    }
    private void Start()
    {
        currentHeath = Heath;

        #region Set point
        // bắn raycast để xác định đúng position của mặt đất
        RaycastHit2D hitInfor = Physics2D.Raycast(transform.position, Vector2.down, 10, ground);
        if (hitInfor)
            groundContact = hitInfor.point;

        // gán lại tất cả điểm di chuyển cho nó trùng với mặt đất
        foreach (Transform point in transformMovePoints)
        {
            point.position = new Vector2(point.position.x, groundContact.y);
        }
        movementPoint = transformMovePoints.Select(transform => transform.position).ToArray();
        currentPointIndex = 0;
        currentPoint = movementPoint[currentPointIndex];
        #endregion
    }
    private void Update()
    {
        #region Check
        isOnGround = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, ground) != null;

        isOnFollow = (Physics2D.OverlapCollider(PreviousCollider, playerContactFillter, colliders) != 0
            || Physics2D.OverlapCollider(BehindCollider, playerContactFillter, colliders) != 0) &&
            !knight.IsDie;

        inAttackRage = Physics2D.OverlapBox(attackRagePoint.position, attackRageSize, 0, PlayerLayer);
        #endregion

        #region
        currentPositionOnGround = new Vector2(transform.position.x, currentPoint.y);
        if (isOnGround && !IsDie)
        {
            if (!isOnFollow)
            {
                direction = (currentPoint - currentPositionOnGround).normalized;

                // nếu nó không nghỉ thì nó di chuyển =))
                if (!isStandRest)
                {
                    isWalking = true;
                    animator.SetBool("Walk", true);
                }

                // nếu đi đến 1 point thì nó sẽ dừng lại nghỉ
                if (Vector2.Distance(currentPositionOnGround, currentPoint) < 0.1f && !isStandRest)
                {
                    animator.SetBool("Walk", false);
                    isStandRest = true;
                    isWalking = false;
                    rb.velocity = Vector2.zero;
                }

                // đếm ngược thời gian nghỉ
                if (isStandRest)
                {
                    currentRestingTime += Time.deltaTime;
                    if (currentRestingTime >= restTime)
                    {
                        // gán lại điểm tiếp theo nó di chuyển tới;
                        currentPointIndex++;
                        if (currentPointIndex >= movementPoint.Length)
                            currentPointIndex = 0;

                        currentPoint = movementPoint[currentPointIndex];
                        isStandRest = false;
                        currentRestingTime = 0;
                    }
                }
                
            }
            else
            {
                if (!inAttackRage && !isAttacking)
                {
                    KnightPosOnGround = new Vector2(knight.transform.position.x, groundContact.y);
                    direction = (KnightPosOnGround - currentPositionOnGround).normalized;
                    animator.SetBool("Walk", true);
                    isWalking = true;
                }
                else
                {
                    isWalking = false;
                    rb.velocity = Vector2.zero;
                    animator.SetBool("Walk", false);
                    if (!isAttacking)
                    {
                        isAttacking = true;
                        animator.SetTrigger("Attack");
                    }
                }

            }
            // xoay người   
            if (isWalking && !isAttacking)
                TurnAround();

            if (IsDie)
            {
                isWalking = false;
                isAttacking = false;
                animator.SetTrigger("Dead");
            }
        }
        #endregion
    }
    private void FixedUpdate()
    {
        if (isWalking)
            Walk();
    }

    void TurnAround()
    {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                    rb.velocity.x < 0 ? 180 : 0, transform.eulerAngles.z);
    }
    void Walk()
    {
        rb.velocity = direction * MoveSpeed;
    }

    #region Attack
    void Attacking()
    {
        List<Collider2D> hits = new List<Collider2D>();
        int hitsLength = Physics2D.OverlapCollider(weaponCollider, playerContactFillter, hits);

        if(hitsLength > 0 && hits[0].CompareTag("Player"))
        {
            hits[0].GetComponent<Knight>().TakeDame(Damage);
        }
    }
    void EndAttacking()
    {
        if (isAttacking)
            isAttacking = false;
    }
    #endregion

    public void TakeDamage(float damage)
    {
        currentHeath -= damage;
        animator.SetTrigger("Hit");
    }
    void Die()
    {
        Destroy(gameObject);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.DrawCube(attackRagePoint.position, attackRageSize);
    }
}
