using UnityEngine;

public class TheStack : MonoBehaviour
{
    // Const Value
    private const float BoundSize = 3.5f;
    private const float MovingBoundsSize = 3f;
    private const float StackMovingSpeed = 5.0f;
    private const float BlockMovingSpeed = 3.5f;
    private const float ErrorMargin = 0.1f;

    public GameObject originBlock = null;

    private Vector3 prevBlockPosition;
    private Vector3 desiredPosition;
    private Vector3 stackBounds = new Vector2(BoundSize, BoundSize);

    Transform lastBlock = null;
    float blockTransition = 0f;
    float secondaryPosition = 0f;

    int stackCount = -1;
    int comboCount = 0;

    public Color prevColor;
    public Color nextColor;

    void Start()
    {
        if (originBlock == null)
        {
            Debug.Log("OriginBlock is NULL");
            return;
        }

        prevColor = GetRandomColor();
        nextColor = GetRandomColor();

        prevBlockPosition = Vector3.down;
        Spawn_Block();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Spawn_Block();
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, StackMovingSpeed * Time.deltaTime);
    }

    bool Spawn_Block()
    {
        // 이전블럭 저장
        if (lastBlock != null)
            prevBlockPosition = lastBlock.localPosition;

        GameObject newBlock = null;
        Transform newTrans = null;

        newBlock = Instantiate(originBlock);

        if (newBlock == null)
        {
            Debug.Log("NewBlock Instantiate Failed!");
            return false;
        }

        ColorChange(newBlock);

        newTrans = newBlock.transform;
        newTrans.parent = this.transform; // 부모 없이 새롭게 생성된 오브젝트의 부모는 this의 트랜스폼으로 설정
        newTrans.localPosition = prevBlockPosition + Vector3.up;
        newTrans.localRotation = Quaternion.identity; // Quaternion의 초기값
        newTrans.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);

        stackCount++;

        desiredPosition = Vector3.down * stackCount;
        blockTransition = 0f;

        lastBlock = newTrans;

        return true;
    }

    Color GetRandomColor()
    {
        float r = Random.Range(100f, 250f) / 255f; // 100보다 낮게하면 너무 어두운 색이 나올 수 있음
        float g = Random.Range(100f, 250f) / 255f;
        float b = Random.Range(100f, 250f) / 255f;

        return new Color(r, g, b);
    }

    void ColorChange(GameObject go)
    {
        Color applyColor = Color.Lerp(prevColor, nextColor, (stackCount % 11) / 10f);

        Renderer rn = go.GetComponent<Renderer>();

        if (rn == null)
        {
            Debug.Log("Renderer is NULL!");
            return;
        }

        rn.material.color = applyColor;
        Camera.main.backgroundColor = applyColor - new Color(0.1f, 0.1f, 0.1f);

        if (applyColor.Equals(nextColor) == true)
        {
            prevColor = nextColor;
            nextColor = GetRandomColor();
        }
    }
}
