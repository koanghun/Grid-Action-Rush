using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// オブジェクトプールを管理するシングルトンマネージャー
/// WebGL環境でのGC対策として、頻繁に生成/破棄されるオブジェクトを再利用
/// </summary>
public class PoolManager : MonoBehaviour
{
    #region 定義

    [Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
    }

    #endregion

    #region シングルトン

    private static PoolManager instance;

    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<PoolManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("PoolManager");
                    instance = obj.AddComponent<PoolManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region Inspector設定

    [Header("プール設定")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

    #endregion

    #region 内部状態

    // プレハブごとにプールを保持するディクショナリ
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    // 生成されたオブジェクトの親コンテナ（Hierarchy整理用）
    private Dictionary<GameObject, Transform> poolParents = new Dictionary<GameObject, Transform>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// 設定されたプールを初期化（プレウォーム）
    /// </summary>
    private void InitializePools()
    {
        foreach (PoolConfig config in poolConfigs)
        {
            if (config.prefab == null) continue;

            CreatePool(config.prefab, config.initialSize);
        }
    }

    /// <summary>
    /// 個別のプールを作成・拡張
    /// </summary>
    private void CreatePool(GameObject prefab, int size)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();

            // Hierarchy整理用の親オブジェクト作成
            GameObject parentObj = new GameObject($"Pool_{prefab.name}");
            parentObj.transform.SetParent(transform);
            poolParents[prefab] = parentObj.transform;
        }

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolParents[prefab]);
            obj.SetActive(false);
            poolDictionary[prefab].Enqueue(obj);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// プールからオブジェクトを取得
    /// プールが空の場合は新規生成
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            // 未登録の場合はデフォルトサイズで新規作成
            CreatePool(prefab, 1);
        }

        GameObject obj;

        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
        }
        else
        {
            // 足りない場合は新規生成（親を指定）
            Transform parent = poolParents.ContainsKey(prefab) ? poolParents[prefab] : transform;
            obj = Instantiate(prefab, parent);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// オブジェクトをプールに返却
    /// </summary>
    public void Return(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);

        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab, 0); // 辞書登録のみ
        }

        poolDictionary[prefab].Enqueue(obj);
        
        // 親を元に戻す（もし変更されていた場合）
        if (poolParents.ContainsKey(prefab))
        {
            obj.transform.SetParent(poolParents[prefab]);
        }
    }

    #endregion
}
