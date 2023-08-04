using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGround : MonoBehaviour
{
    public Vector3 PosOffset;
    Transform mainCamera;
    Vector3 cameraStartPosition;
    float distance;

    GameObject[] BackGrounds;
    Material[] materials;
    float[] backSpeeds;

    float farthesBackGround;

    [Range(0.01f, 0.05f)] public float ParallaxSpeed;

    private void Start()
    {
        mainCamera = Camera.main.transform;
        cameraStartPosition = transform.position;

        int backCount = transform.childCount;
        materials = new Material[backCount];
        backSpeeds = new float[backCount];
        BackGrounds = new GameObject[backCount];

        for(int index = 0 ; index < backCount; index++)
        {
            BackGrounds[index] = transform.GetChild(index).gameObject;
            materials[index] = BackGrounds[index].GetComponent<Renderer>().material;
        }
        BackSpeedCaculate();
    }

    void BackSpeedCaculate()
    {
        foreach(GameObject BackGround in BackGrounds)
        {
            if((BackGround.transform.position.z - mainCamera.position.z) > farthesBackGround)
            {
                farthesBackGround = BackGround.transform.position.z - mainCamera.position.z;
            }
        }
        
        for(int index = 0; index < BackGrounds.Length; index++)
        {
            backSpeeds[index] = 1 - (BackGrounds[index].transform.position.z - mainCamera.position.z)/farthesBackGround;
        }
    }

    private void LateUpdate()
    {
        distance = mainCamera.position.x - cameraStartPosition.x;
        transform.position = new Vector3(mainCamera.position.x, mainCamera.position.y, 0) + PosOffset;
        for(int index = 0 ;index < BackGrounds.Length ; index++)
        {
            float speed = backSpeeds[index] * ParallaxSpeed;
            materials[index].SetTextureOffset("_MainTex", new Vector2(distance, 0) * speed);
        }
    }
}
