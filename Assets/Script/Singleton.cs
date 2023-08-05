using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    // khởi tạo thể hiện duy nhất của lớp
    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                // tìm kiếm các thể hiện của class T trong scene
                instance = FindObjectOfType<T>();
                // nếu không tìm thấy thì tạo 1 object mới và gắn lớp T vào đó 
                GameObject singletonObject = new GameObject(typeof(T).Name);
                instance = singletonObject.AddComponent<T>();
            }
            return instance;
        }
    }
    protected virtual void Awake()
    {
        MakeSingleton(true);
    }

    //hủy hoặc giữ lại đối tượng cũ khi load scene  
    protected void MakeSingleton(bool destroyOnLoad)
    {
        if(instance == null)
        {
            instance = this as T;
            if (destroyOnLoad)
            {
                var root = transform.root;
                if(root != transform)
                {
                    DontDestroyOnLoad(root);
                }
                else
                {
                    DontDestroyOnLoad(this.gameObject);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
