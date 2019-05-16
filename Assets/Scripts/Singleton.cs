using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T mInstance;

    public virtual void Awake()
    {
        if (mInstance == null)
        {
            mInstance = this as T;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static T Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = GameObject.FindObjectOfType<T>();

                if (mInstance == null)
                {
                    GameObject singleton = new GameObject(typeof(T).Name);
                    mInstance = singleton.AddComponent<T>();
                }
            }

            return mInstance;
        }
    }
}