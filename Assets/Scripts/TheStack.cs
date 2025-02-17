using UnityEngine;

public class TheStack : MonoBehaviour
{
    private const float BoundSize = 3.5f; // ��ü ���� �ִ� ũ��
    private const float MovingBoundsSize = 3f; // �̵��ϴ� ���� �̵� ����
    private const float StackMovingSpeed = 5.0f;
    private const float BlockMovingSpeed = 3.5f;
    private const float ErrorMargin = 0.1f;

    public GameObject originBlock = null;

    private Vector3 prevBlockPosition; // �ٷ� �Ʒ��� �ִ� ���� ��ġ
    private Vector3 desiredPosition; // ���� ��ü�� �̵��� ��ǥ ��ġ
    private Vector3 stackBounds = new Vector2(BoundSize, BoundSize); // ���� ���� ũ�� ����

    Transform lastBlock = null; // ���������� ������ ���� Transform
    float blockTransition = 0f; // �� �̵� �ִϸ��̼ǿ� ���Ǵ� �ð� ����
    float secondaryPosition = 0f; // �� ��° ��(x �Ǵ� z)�� ������ ��ġ ��

    int stackCount = -1; // ���ÿ� ���� ���� �� (-1���� �����Ͽ� Spawn_Block() �� ����)
    public int Score { get { return stackCount; } }
    int comboCount = 0;
    public int Combo { get { return comboCount; } }
    int maxCombo;
    public int MaxCombo { get => maxCombo; }
    public Color prevColor;
    public Color nextColor;

    bool isMovingX = true; // �� �̵� ������ X������ ���� (false�� Z��)

    int bestScore = 0;
    public int BestScore { get => bestScore; }
    int bestCombo = 0;
    public int BestCombo { get => bestCombo; }

    // PlayerPrefs�� ������ Ű��
    private const string BestScoreKey = "BestScore";
    private const string BestComboKey = "BestCombo";

    private bool isGameOver = false;

    // ���� ���� �� �ʱ� ���� �� ù �� ����
    void Start()
    {
        if (originBlock == null)
        {
            Debug.Log("OriginBlock is NULL");
            return;
        }

        // ����� �ְ� ������ �޺� �ҷ�����
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        bestCombo = PlayerPrefs.GetInt(BestComboKey, 0);

        // �ʱ� ���� ����: ���� ���� �Ҵ�
        prevColor = GetRandomColor();
        nextColor = GetRandomColor();

        // �ʱ� ���� �� ��ġ�� (0, -1, 0)���� ����
        prevBlockPosition = Vector3.down;

        // ���� �� 2�� ���� (ù ���� ������ �ٴ� ����)
        Spawn_Block();
        Spawn_Block();
    }

    // �� �̵� �� �Է� ó��
    void Update()
    {
        if (isGameOver) return; // ���� ���� �� ������Ʈ ����

        // ���콺 Ŭ�� �Է� ���� (���� �ױ� ���� �Է�)
        if (Input.GetMouseButtonDown(0))
        {
            // ���� �ùٸ��� ������ �� �� ����, �ƴϸ� ���� ���� ó��
            if (PlaceBlock())
            {
                Spawn_Block();
            }
            else
            {
                Debug.Log("Game Over");
                UpdateScore();       // �ְ� ���� ��� ������Ʈ
                isGameOver = true;
                GameOverEffect();    // ���� ���� �� �� ���� ȿ�� ����
                UIManager.Instance.SetScoreUI(); // UI ������Ʈ
            }
        }

        // ���� �����̴� ���� �̵� ó��
        MoveBlock();
        // ���� ��ü�� ��ġ�� �ε巴�� ����(Lerp)�Ͽ� �ִϸ��̼� ȿ�� ����
        transform.position = Vector3.Lerp(transform.position, desiredPosition, StackMovingSpeed * Time.deltaTime);
    }

    // ���ο� ���� �����ϰ� �ʱ�ȭ
    bool Spawn_Block()
    {
        // ���� ���� ��ġ�� ���� (�� ���� ���� �� ���� ������)
        if (lastBlock != null)
            prevBlockPosition = lastBlock.localPosition;

        GameObject newBlock = null;
        Transform newTrans = null;

        // ���� �� �������� �����Ͽ� �� �� ����
        newBlock = Instantiate(originBlock);

        if (newBlock == null)
        {
            Debug.Log("NewBlock Instantiate Failed!");
            return false;
        }

        // �� ���� ���� ���� ����
        ColorChange(newBlock);

        newTrans = newBlock.transform;
        newTrans.parent = this.transform; // ������ ���� ���� ������ �ڽ����� ����
        newTrans.localPosition = prevBlockPosition + Vector3.up; // ���� �� ���� ��ġ
        newTrans.localRotation = Quaternion.identity; // ȸ�� �ʱ�ȭ
        newTrans.localScale = new Vector3(stackBounds.x, 1, stackBounds.y); // ���� �� ũ�� ����

        stackCount++; // ���ÿ� �� �ϳ� �߰�

        // ���� ��ü�� ��ǥ ��ġ ������Ʈ (�Ʒ��� �̵�)
        desiredPosition = Vector3.down * stackCount;
        blockTransition = 0f; // �� �̵� �ִϸ��̼� �ʱ�ȭ

        lastBlock = newTrans; // ������ ������ �� ����

        // ���� ���� �̵� ������ �ݴ�� ��ȯ (X��� Z�� ������)
        isMovingX = !isMovingX;

        // UI�� ���� ���� ������Ʈ
        UIManager.Instance.UpdateScore();
        return true;
    }

    // 100~250 ������ ���� ���� ���� (��ο� �� ����)
    Color GetRandomColor()
    {
        float r = Random.Range(100f, 250f) / 255f;
        float g = Random.Range(100f, 250f) / 255f;
        float b = Random.Range(100f, 250f) / 255f;

        return new Color(r, g, b);
    }

    // �� ����� ī�޶� ���� ����
    void ColorChange(GameObject go)
    {
        // ���� ���� ���� ���� ����� ���� ���� ���̸� ����(Lerp)�Ͽ� ������ ���� ����
        Color applyColor = Color.Lerp(prevColor, nextColor, (stackCount % 11) / 10f);

        Renderer rn = go.GetComponent<Renderer>();

        if (rn == null)
        {
            Debug.Log("Renderer is NULL!");
            return;
        }

        // ���� ������ ���� ����
        rn.material.color = applyColor;
        // ī�޶� ������ �� ���󺸴� �ణ ��Ӱ� ����
        Camera.main.backgroundColor = applyColor - new Color(0.1f, 0.1f, 0.1f);

        // ���� �� ������ ���� ����(nextColor)�� ��������, ���� ��ȯ�� ���� prevColor�� nextColor ������Ʈ
        if (applyColor.Equals(nextColor) == true)
        {
            prevColor = nextColor;
            nextColor = GetRandomColor();
        }
    }

    // ���� ������ ���� ������ ���� ������ �պ� �̵���Ű�� �Լ�
    void MoveBlock()
    {
        // �ð��� ���� �� �̵� ����� ����
        blockTransition += Time.deltaTime * BlockMovingSpeed;

        // PingPong �Լ��� ����Ͽ� ���� ������ ���� ������ �պ� �̵��ϵ��� ��
        float movePosition = Mathf.PingPong(blockTransition, BoundSize) - BoundSize / 2;

        if (isMovingX)
        {
            // X������ �̵��ϴ� ���: secondaryPosition�� ���� z�� ����
            lastBlock.localPosition = new Vector3(movePosition * MovingBoundsSize, stackCount, secondaryPosition);
        }
        else
        {
            // Z������ �̵��ϴ� ���: secondaryPosition�� ���� x�� ���� (���� ��ȣ ����)
            lastBlock.localPosition = new Vector3(secondaryPosition, stackCount, -movePosition * MovingBoundsSize);
        }
    }

    // ���� �����̴� ���� ������Ű��, ���� ������ ���� ���� �߶󳻰ų� �޺� ó��
    // - ���� ��Ȯ�� ������ �޺� ���� �� ��ġ ����
    // - �� ��ġ ������ Ŭ ��� ���� �κ��� �߶󳻰�, ���� ���� ũ�⸦ ����
    // - ���� �� ũ�Ⱑ 0 �����̸� ���� ����
    bool PlaceBlock()
    {
        Vector3 lastPosition = lastBlock.localPosition;

        if (isMovingX)
        {
            // X�� �̵��� ���: ���� ������ X��ǥ ���� ���
            float deltaX = prevBlockPosition.x - lastPosition.x;
            bool isNegativeNum = (deltaX < 0) ? true : false;

            deltaX = Mathf.Abs(deltaX);
            if (deltaX > ErrorMargin)
            {
                // ���� ������ �Ѿ�� ���� X ũ�⸦ ���ҽ�Ŵ
                stackBounds.x -= deltaX;
                if (stackBounds.x <= 0)
                {
                    return false; // ���� �� ũ�Ⱑ 0 �����̸� ���� ����
                }

                // ���� ���� �߾� ��ġ ��� ��, �� ������ �� ��ġ ����
                float middle = (prevBlockPosition.x + lastPosition.x) / 2;
                lastBlock.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);

                Vector3 tempPosition = lastBlock.localPosition;
                tempPosition.x = middle;
                lastBlock.localPosition = lastPosition = tempPosition;

                // �߷��� �κ�(���)�� �����Ͽ� ����߸�
                float rubbleHalfScale = deltaX / 2f;
                CreateRubble(
                    new Vector3(isNegativeNum
                        ? lastPosition.x + stackBounds.x / 2 + rubbleHalfScale
                        : lastPosition.x - stackBounds.x / 2 - rubbleHalfScale,
                    lastPosition.y,
                    lastPosition.z),
                    new Vector3(deltaX, 1, stackBounds.y)
                );

                comboCount = 0; // �߸� �׾����Ƿ� �޺� ����
            }
            else
            {
                // ���� ���� ���� ��Ȯ�� ���� ��� �޺� ���� ó�� �� ��ġ ����
                ComboCheck();
                lastBlock.localPosition = prevBlockPosition + Vector3.up;
            }
        }
        else
        {
            // Z�� �̵��� ���: ���� ������ Z��ǥ ���� ���
            float deltaZ = prevBlockPosition.z - lastPosition.z;
            bool isNegativeNum = (deltaZ < 0) ? true : false;

            deltaZ = Mathf.Abs(deltaZ);
            if (deltaZ > ErrorMargin)
            {
                // ���� ������ ������ ���� Z ũ�⸦ ���ҽ�Ŵ
                stackBounds.y -= deltaZ;
                if (stackBounds.y <= 0)
                {
                    return false; // ���� �� ũ�Ⱑ 0 �����̸� ���� ����
                }

                // �߾� ��ġ ��� ��, ������ �� ��ġ ����
                float middle = (prevBlockPosition.z + lastPosition.z) / 2f;
                lastBlock.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);

                Vector3 tempPosition = lastBlock.localPosition;
                tempPosition.z = middle;
                lastBlock.localPosition = lastPosition = tempPosition;

                float rubbleHalfScale = deltaZ / 2f;
                CreateRubble(
                    new Vector3(lastPosition.x, lastPosition.y,
                    isNegativeNum
                        ? lastPosition.z + stackBounds.y / 2 + rubbleHalfScale
                        : lastPosition.z - stackBounds.y / 2 - rubbleHalfScale),
                    new Vector3(stackBounds.x, 1, deltaZ)
                );

                comboCount = 0; // �޺� ����
            }
            else
            {
                // ��Ȯ�� ���� ��� �޺� ���� �� ��ġ ����
                ComboCheck();
                lastBlock.localPosition = prevBlockPosition + Vector3.up;
            }
        }

        // ���� ���� �̵� �� ������ �� �� ��° ���� �� ������Ʈ
        secondaryPosition = (isMovingX) ? lastBlock.localPosition.x : lastBlock.localPosition.z;

        return true;
    }

    // ������ �����ϰ� ���� ȿ���� ����
    void CreateRubble(Vector3 pos, Vector3 scale)
    {
        // ������ ���� �����Ͽ� ���� ������Ʈ ����
        GameObject go = Instantiate(lastBlock.gameObject);
        go.transform.parent = this.transform;

        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.identity;

        // Rigidbody �߰� �� �߷¿� ���� �������� ��
        go.AddComponent<Rigidbody>();
        go.name = "Rubble";
    }

    // �������� ��Ȯ�ϰ� ���� ���� ��� �޺� ���� ������Ű�� 5�޺����� ���� ũ�⸦ ���ݾ� ������Ű�� �Լ�
    void ComboCheck()
    {
        comboCount++;

        if (comboCount > maxCombo) maxCombo = comboCount;

        // 5�޺� �޼� ��, �� ũ�⸦ ���� (�ִ� BoundSize����)
        if ((comboCount % 5) == 0)
        {
            Debug.Log("5 Combo Success!");
            stackBounds += new Vector3(0.5f, 0.5f);
            stackBounds.x = (stackBounds.x > BoundSize) ? BoundSize : stackBounds.x;
            stackBounds.y = (stackBounds.y > BoundSize) ? BoundSize : stackBounds.y;
        }
    }

    // ���� ���� ���� �ְ� �������� ������ �ְ� ������ �޺��� ������Ʈ�ϰ� ����
    void UpdateScore()
    {
        if (bestScore < stackCount)
        {
            Debug.Log("�ְ� ���� ����");
            bestScore = stackCount;
            bestCombo = maxCombo;

            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.SetInt(BestComboKey, bestCombo);
        }
    }


    //  ���� ���� �� ���� n ���� ���� Rigidbody�� �߰��Ͽ� �������� ����� ȿ��
    void GameOverEffect()
    {
        int childCount = this.transform.childCount;

        // �ֱ� 20���� ���� ���� ȿ�� ���� (���ش� ����)
        for (int i = 1; i < 20; i++)
        {
            if (childCount < i) break;

            GameObject go = transform.GetChild(childCount - i).gameObject;

            if (go.name.Equals("Rubble")) continue;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.AddForce(
                (Vector3.up * Random.Range(0, 10f) + Vector3.right * (Random.Range(0, 10f) - 5f)) * 100f
            );
        }
    }

    // (���� ���� ���� �� �ʱ� ���� ����)
    public void Restart()
    {
        int childCount = transform.childCount;

        // ��� �ڽ� ������Ʈ(��) ����
        for (int i = 0; i < childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // ���� ���� ���¿� ���� ������ �ʱ�ȭ
        isGameOver = false;
        lastBlock = null;
        desiredPosition = Vector3.zero;
        stackBounds = new Vector3(BoundSize, BoundSize);

        stackCount = -1;
        isMovingX = true;
        blockTransition = 0f;
        secondaryPosition = 0f;

        comboCount = 0;
        maxCombo = 0;

        prevBlockPosition = Vector3.down;

        // ���� �ʱ�ȭ
        prevColor = GetRandomColor();
        nextColor = GetRandomColor();

        // �ʱ� �� 2�� ����
        Spawn_Block();
        Spawn_Block();
    }
}
