using UnityEngine;

/// <summary>
/// 1フレーム キャラクター スプライトを制御する アニメーション コントローラー
/// </summary>
public class CharacterAnimation : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private GridMovementController movementController;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("バウンドアニメーション設定")]
    [SerializeField] private float bounceSpeed = 15f;
    [SerializeField] private float bounceHeight = 0.15f;

    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (movementController == null)
            movementController = GetComponentInParent<GridMovementController>();
    }

    private void Update()
    {
        if (movementController == null || spriteRenderer == null) return;

        // 1. 向いている方向に応じて左右反転 (Flip)
        Vector2Int dir = movementController.FacingDirection;
        if (dir.x > 0) spriteRenderer.flipX = false;
        else if (dir.x < 0) spriteRenderer.flipX = true;

        // 2. 移動中跳ねるバウンド効果適用
        if (movementController.IsMoving)
        {
            // 絶対値 Sin 関数を用いて地面 위로 only に跳ねるように処理
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            transform.localPosition = initialLocalPos + new Vector3(0, bounce, 0);
        }
        else
        {
            // 停止状態のときは滑らかに元の位置に戻る
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos, Time.deltaTime * 10f);
        }
    }
}
