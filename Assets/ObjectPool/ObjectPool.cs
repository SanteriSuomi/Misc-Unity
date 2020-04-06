using System.Collections.Generic;

public class ObjectPool<T> : Singleton<ObjectPool<T>> where T : Component
{
    private Queue<T> pool;
    private Transform poolParent;
    [SerializeField]
    private T prefabToPool = default;
    [SerializeField]
    private int poolSize = 25;

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
    }

    private void InitializePool()
    {
        pool = new Queue<T>(poolSize);
        AddParent();
        for (int i = 0; i < poolSize; i++)
        {
            NewPoolObj();
        }
    }

    private void AddParent()
    {
        string parentName = $"{typeof(T).Name} Pool Objects";
        if (GameObject.Find(parentName) == null)
        {
            poolParent = new GameObject($"{typeof(T).Name} Pool Objects").transform;
            if (poolParent.transform.parent is null)
            {
                DontDestroyOnLoad(poolParent);
            }
        }
    }

    /// <summary>
    /// Get a object from the pool.
    /// </summary>
    /// <returns></returns>
    public T Get()
    {
        CheckObjValidity(pool.Peek());
        T obj = pool.Dequeue();
        Activate(obj, true);
        return obj;
    }

    /// <summary>
    /// Return a object to the pool.
    /// </summary>
    /// <param name="obj"></param>
    public void Return(T obj)
    {
        Activate(obj, false);
        pool.Enqueue(obj);
    }

    private void CheckObjValidity(T obj)
    {
        if (obj is null)
        {
            NewPoolObj();
        }
        else if (obj.gameObject.activeSelf)
        {
            NewPoolObj();
        }
    }

    private void NewPoolObj()
    {
        T obj = Instantiate(prefabToPool);
        Activate(obj, false);
        obj.transform.SetParent(poolParent);
        pool.Enqueue(obj);
    }

    private void Activate(T obj, bool activate)
        => obj.gameObject.SetActive(activate);
}
