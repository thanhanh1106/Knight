using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Skeleton : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    AnimationController animationController;

    public float MoveSpeed;

    [SerializeField] LayerMask ground;
    [SerializeField] LayerMask PlayerLayer;
    [SerializeField] Transform groundCheckPoint;
    [SerializeField] Vector2 groundCheckSize;
    [SerializeField] Transform[] transformMovePoints;
    [SerializeField] Transform weaponPoint1;
    [SerializeField] Transform weaponPoint2;

    Vector3[] movementPoint;
    Vector2 groundContact;

    Vector2 currentPoint;
    int currentPointIndex;
    Vector2 currentPositionOnGround;
    Vector2 direction;

    float currentRestingTime;
    float restTime = 1.5f;

    bool isOnGround;
    bool isOnFollow;
    bool isStandRest;
    bool isWalking;
    bool isAttacking;

    public bool IsDie => currentHeath <= 0;

    [SerializeField] float Heath;
    float currentHeath;
    [SerializeField] float Damage;
    [SerializeField] float AttackingTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animationController = new AnimationController(animator);

        // bắn raycast để xác định đúng position của mặt đất
        RaycastHit2D hitInfor = Physics2D.Raycast(transform.position, Vector2.down,10,ground);
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
    }
    private void Update()
    {
        #region Check
        isOnGround = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, ground) != null;
        #endregion

        #region
        currentPositionOnGround = new Vector2(transform.position.x, currentPoint.y);
        if (isOnGround)
        {
            if (!isOnFollow)
            {
                direction = (currentPoint - currentPositionOnGround).normalized;

                // nếu nó không nghỉ thì nó di chuyển =))
                if (!isStandRest)
                {
                    isWalking = true;
                }

                // nếu đi đến 1 point thì nó sẽ dừng lại nghỉ
                if (Vector2.Distance(currentPositionOnGround, currentPoint) < 0.1f && !isStandRest)
                {
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
                // xoay người   
                if (isWalking)
                    TurnAround();
            }
        }
        #endregion

        #region AnimationHandler
        if (isStandRest)
            animationController.ChangeAnimationState("Idle");

        if(isWalking)
            animationController.ChangeAnimationState("Walk");
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
    public void TakeDame(float damage)
    {
        currentHeath -= damage;
    }

    void Attack()
    {
        isAttacking = true;
        StartCoroutine(Attacking());
    }
    IEnumerator Attacking()
    {
        float startTime = Time.time;
        bool isDame = false; // dùng để kiểm tra xem người chơi bị nhận dame hay chưa, nếu nhận rồi thì bỏ qua
        while(Time.time - startTime < AttackingTime)
        {
            Vector2 weaponDirection = weaponPoint2.position - weaponPoint1.position;
            RaycastHit2D hit = Physics2D.Raycast(weaponPoint1.position,weaponDirection,weaponDirection.magnitude,PlayerLayer);
            if (hit && !isDame)
            {
                hit.collider.gameObject.GetComponent<Knight>().TakeDame(Damage);
                isDame = true;
            }
            yield return null;
        }
        isDame = true;
        isAttacking = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(groundCheckPoint.position, groundCheckSize);
    }
}
