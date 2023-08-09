using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform targetTranform;
    [SerializeField] Vector3 fixPosition;
    [SerializeField] float smoothTime;
    Vector3 velocity;
    private void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetTranform.position + fixPosition, ref velocity, smoothTime);
    }
}
