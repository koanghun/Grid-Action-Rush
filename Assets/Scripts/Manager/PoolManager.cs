using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// オブジェクトプールを管理するシングルトンマネージャー
/// WebGL環境でのGC対策として、頻繁に生成/破棄されるオブジェクトを再利用
/// </summary>
public class PoolManager : MonoBehaviour
{
    #region シングルトン

    public static PoolManager Instance { get; private set; }

    #endregion

    #region プール管理

    // プレハブごとにプールを保持するディクショナリ
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン初期化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// プールからオブジェクトを取得
    /// プールが空の場合は新規生成
    /// </summary>
    /// <param name="prefab">生成元プレハブ</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成回転</param>
    /// <returns>取得したGameObject</returns>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // プレハブが未登録の場合は新規登録
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        GameObject obj;

        // プールに利用可能なオブジェクトがあれば再利用
        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            // プールが空なら新規生成
            obj = Instantiate(prefab, position, rotation);
        }

        return obj;
    }

    /// <summary>
    /// オブジェクトをプールに返却
    /// </summary>
    /// <param name="prefab">元のプレハブ</param>
    /// <param name="obj">返却するオブジェクト</param>
    public void Return(GameObject prefab, GameObject obj)
    {
        // 返却時は非アクティブ化
        obj.SetActive(false);

        // プールに追加
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        poolDictionary[prefab].Enqueue(obj);
    }

    #endregion
}
