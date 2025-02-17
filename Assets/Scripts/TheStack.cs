using UnityEngine;

public class TheStack : MonoBehaviour
{
    private const float BoundSize = 3.5f; // 전체 블럭의 최대 크기
    private const float MovingBoundsSize = 3f; // 이동하는 블럭의 이동 범위
    private const float StackMovingSpeed = 5.0f;
    private const float BlockMovingSpeed = 3.5f;
    private const float ErrorMargin = 0.1f;

    public GameObject originBlock = null;

    private Vector3 prevBlockPosition; // 바로 아래에 있는 블럭의 위치
    private Vector3 desiredPosition; // 스택 전체가 이동할 목표 위치
    private Vector3 stackBounds = new Vector2(BoundSize, BoundSize); // 현재 블럭의 크기 범위

    Transform lastBlock = null; // 마지막으로 생성된 블럭의 Transform
    float blockTransition = 0f; // 블럭 이동 애니메이션에 사용되는 시간 변수
    float secondaryPosition = 0f; // 두 번째 축(x 또는 z)의 고정된 위치 값

    int stackCount = -1; // 스택에 쌓인 블럭의 수 (-1에서 시작하여 Spawn_Block() 시 증가)
    public int Score { get { return stackCount; } }
    int comboCount = 0;
    public int Combo { get { return comboCount; } }
    int maxCombo;
    public int MaxCombo { get => maxCombo; }
    public Color prevColor;
    public Color nextColor;

    bool isMovingX = true; // 블럭 이동 방향이 X축인지 여부 (false면 Z축)

    int bestScore = 0;
    public int BestScore { get => bestScore; }
    int bestCombo = 0;
    public int BestCombo { get => bestCombo; }

    // PlayerPrefs에 저장할 키값
    private const string BestScoreKey = "BestScore";
    private const string BestComboKey = "BestCombo";

    private bool isGameOver = false;

    // 게임 시작 시 초기 설정 및 첫 블럭 생성
    void Start()
    {
        if (originBlock == null)
        {
            Debug.Log("OriginBlock is NULL");
            return;
        }

        // 저장된 최고 점수와 콤보 불러오기
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        bestCombo = PlayerPrefs.GetInt(BestComboKey, 0);

        // 초기 색상 설정: 랜덤 색상 할당
        prevColor = GetRandomColor();
        nextColor = GetRandomColor();

        // 초기 이전 블럭 위치를 (0, -1, 0)으로 설정
        prevBlockPosition = Vector3.down;

        // 최초 블럭 2개 생성 (첫 블럭은 스택의 바닥 역할)
        Spawn_Block();
        Spawn_Block();
    }

    // 블럭 이동 및 입력 처리
    void Update()
    {
        if (isGameOver) return; // 게임 오버 시 업데이트 중지

        // 마우스 클릭 입력 감지 (블럭을 쌓기 위한 입력)
        if (Input.GetMouseButtonDown(0))
        {
            // 블럭을 올바르게 놓으면 새 블럭 생성, 아니면 게임 오버 처리
            if (PlaceBlock())
            {
                Spawn_Block();
            }
            else
            {
                Debug.Log("Game Over");
                UpdateScore();       // 최고 점수 기록 업데이트
                isGameOver = true;
                GameOverEffect();    // 게임 오버 시 블럭 물리 효과 적용
                UIManager.Instance.SetScoreUI(); // UI 업데이트
            }
        }

        // 현재 움직이는 블럭의 이동 처리
        MoveBlock();
        // 스택 전체의 위치를 부드럽게 변경(Lerp)하여 애니메이션 효과 적용
        transform.position = Vector3.Lerp(transform.position, desiredPosition, StackMovingSpeed * Time.deltaTime);
    }

    // 새로운 블럭을 생성하고 초기화
    bool Spawn_Block()
    {
        // 이전 블럭의 위치를 저장 (새 블럭은 이전 블럭 위에 생성됨)
        if (lastBlock != null)
            prevBlockPosition = lastBlock.localPosition;

        GameObject newBlock = null;
        Transform newTrans = null;

        // 원본 블럭 프리팹을 복제하여 새 블럭 생성
        newBlock = Instantiate(originBlock);

        if (newBlock == null)
        {
            Debug.Log("NewBlock Instantiate Failed!");
            return false;
        }

        // 새 블럭에 색상 변경 적용
        ColorChange(newBlock);

        newTrans = newBlock.transform;
        newTrans.parent = this.transform; // 생성된 블럭을 현재 스택의 자식으로 설정
        newTrans.localPosition = prevBlockPosition + Vector3.up; // 이전 블럭 위에 위치
        newTrans.localRotation = Quaternion.identity; // 회전 초기화
        newTrans.localScale = new Vector3(stackBounds.x, 1, stackBounds.y); // 현재 블럭 크기 적용

        stackCount++; // 스택에 블럭 하나 추가

        // 스택 전체의 목표 위치 업데이트 (아래로 이동)
        desiredPosition = Vector3.down * stackCount;
        blockTransition = 0f; // 블럭 이동 애니메이션 초기화

        lastBlock = newTrans; // 마지막 생성된 블럭 갱신

        // 다음 블럭은 이동 방향이 반대로 전환 (X축과 Z축 번갈아)
        isMovingX = !isMovingX;

        // UI의 현재 점수 업데이트
        UIManager.Instance.UpdateScore();
        return true;
    }

    // 100~250 범위의 랜덤 색상 생성 (어두운 색 방지)
    Color GetRandomColor()
    {
        float r = Random.Range(100f, 250f) / 255f;
        float g = Random.Range(100f, 250f) / 255f;
        float b = Random.Range(100f, 250f) / 255f;

        return new Color(r, g, b);
    }

    // 블럭 색상과 카메라 배경색 변경
    void ColorChange(GameObject go)
    {
        // 스택 수에 따라 이전 색상과 다음 색상 사이를 보간(Lerp)하여 적용할 색상 결정
        Color applyColor = Color.Lerp(prevColor, nextColor, (stackCount % 11) / 10f);

        Renderer rn = go.GetComponent<Renderer>();

        if (rn == null)
        {
            Debug.Log("Renderer is NULL!");
            return;
        }

        // 블럭의 재질에 색상 적용
        rn.material.color = applyColor;
        // 카메라 배경색을 블럭 색상보다 약간 어둡게 설정
        Camera.main.backgroundColor = applyColor - new Color(0.1f, 0.1f, 0.1f);

        // 만약 블럭 색상이 다음 색상(nextColor)과 같아지면, 색상 전환을 위해 prevColor와 nextColor 업데이트
        if (applyColor.Equals(nextColor) == true)
        {
            prevColor = nextColor;
            nextColor = GetRandomColor();
        }
    }

    // 현재 생성된 블럭을 지정된 범위 내에서 왕복 이동시키는 함수
    void MoveBlock()
    {
        // 시간에 따라 블럭 이동 진행률 증가
        blockTransition += Time.deltaTime * BlockMovingSpeed;

        // PingPong 함수를 사용하여 블럭이 정해진 범위 내에서 왕복 이동하도록 함
        float movePosition = Mathf.PingPong(blockTransition, BoundSize) - BoundSize / 2;

        if (isMovingX)
        {
            // X축으로 이동하는 경우: secondaryPosition은 기존 z값 유지
            lastBlock.localPosition = new Vector3(movePosition * MovingBoundsSize, stackCount, secondaryPosition);
        }
        else
        {
            // Z축으로 이동하는 경우: secondaryPosition은 기존 x값 유지 (음수 부호 적용)
            lastBlock.localPosition = new Vector3(secondaryPosition, stackCount, -movePosition * MovingBoundsSize);
        }
    }

    // 현재 움직이는 블럭을 고정시키고, 맞춘 정도에 따라 블럭을 잘라내거나 콤보 처리
    // - 블럭을 정확히 쌓으면 콤보 증가 및 위치 보정
    // - 블럭 위치 오차가 클 경우 남은 부분을 잘라내고, 남은 블럭의 크기를 줄임
    // - 남은 블럭 크기가 0 이하이면 게임 오버
    bool PlaceBlock()
    {
        Vector3 lastPosition = lastBlock.localPosition;

        if (isMovingX)
        {
            // X축 이동인 경우: 이전 블럭과의 X좌표 차이 계산
            float deltaX = prevBlockPosition.x - lastPosition.x;
            bool isNegativeNum = (deltaX < 0) ? true : false;

            deltaX = Mathf.Abs(deltaX);
            if (deltaX > ErrorMargin)
            {
                // 오차 범위를 넘어서면 블럭의 X 크기를 감소시킴
                stackBounds.x -= deltaX;
                if (stackBounds.x <= 0)
                {
                    return false; // 남은 블럭 크기가 0 이하이면 게임 오버
                }

                // 남은 블럭의 중앙 위치 계산 후, 블럭 스케일 및 위치 보정
                float middle = (prevBlockPosition.x + lastPosition.x) / 2;
                lastBlock.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);

                Vector3 tempPosition = lastBlock.localPosition;
                tempPosition.x = middle;
                lastBlock.localPosition = lastPosition = tempPosition;

                // 잘려진 부분(루블)을 생성하여 떨어뜨림
                float rubbleHalfScale = deltaX / 2f;
                CreateRubble(
                    new Vector3(isNegativeNum
                        ? lastPosition.x + stackBounds.x / 2 + rubbleHalfScale
                        : lastPosition.x - stackBounds.x / 2 - rubbleHalfScale,
                    lastPosition.y,
                    lastPosition.z),
                    new Vector3(deltaX, 1, stackBounds.y)
                );

                comboCount = 0; // 잘못 쌓았으므로 콤보 리셋
            }
            else
            {
                // 오차 범위 내에 정확히 쌓은 경우 콤보 증가 처리 및 위치 보정
                ComboCheck();
                lastBlock.localPosition = prevBlockPosition + Vector3.up;
            }
        }
        else
        {
            // Z축 이동인 경우: 이전 블럭과의 Z좌표 차이 계산
            float deltaZ = prevBlockPosition.z - lastPosition.z;
            bool isNegativeNum = (deltaZ < 0) ? true : false;

            deltaZ = Mathf.Abs(deltaZ);
            if (deltaZ > ErrorMargin)
            {
                // 오차 범위를 넘으면 블럭의 Z 크기를 감소시킴
                stackBounds.y -= deltaZ;
                if (stackBounds.y <= 0)
                {
                    return false; // 남은 블럭 크기가 0 이하이면 게임 오버
                }

                // 중앙 위치 계산 후, 스케일 및 위치 보정
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

                comboCount = 0; // 콤보 리셋
            }
            else
            {
                // 정확히 쌓은 경우 콤보 증가 및 위치 보정
                ComboCheck();
                lastBlock.localPosition = prevBlockPosition + Vector3.up;
            }
        }

        // 다음 블럭의 이동 시 기준이 될 두 번째 축의 값 업데이트
        secondaryPosition = (isMovingX) ? lastBlock.localPosition.x : lastBlock.localPosition.z;

        return true;
    }

    // 파편을 생성하고 물리 효과를 적용
    void CreateRubble(Vector3 pos, Vector3 scale)
    {
        // 마지막 블럭을 복제하여 잔해 오브젝트 생성
        GameObject go = Instantiate(lastBlock.gameObject);
        go.transform.parent = this.transform;

        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.identity;

        // Rigidbody 추가 후 중력에 의해 떨어지게 함
        go.AddComponent<Rigidbody>();
        go.name = "Rubble";
    }

    // 연속으로 정확하게 블럭을 쌓은 경우 콤보 수를 증가시키고 5콤보마다 블럭의 크기를 조금씩 증가시키는 함수
    void ComboCheck()
    {
        comboCount++;

        if (comboCount > maxCombo) maxCombo = comboCount;

        // 5콤보 달성 시, 블럭 크기를 증가 (최대 BoundSize까지)
        if ((comboCount % 5) == 0)
        {
            Debug.Log("5 Combo Success!");
            stackBounds += new Vector3(0.5f, 0.5f);
            stackBounds.x = (stackBounds.x > BoundSize) ? BoundSize : stackBounds.x;
            stackBounds.y = (stackBounds.y > BoundSize) ? BoundSize : stackBounds.y;
        }
    }

    // 현재 스택 수가 최고 점수보다 높으면 최고 점수와 콤보를 업데이트하고 저장
    void UpdateScore()
    {
        if (bestScore < stackCount)
        {
            Debug.Log("최고 점수 갱신");
            bestScore = stackCount;
            bestCombo = maxCombo;

            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.SetInt(BestComboKey, bestCombo);
        }
    }


    //  게임 오버 시 상위 n 개의 블럭에 Rigidbody를 추가하여 떨어지게 만드는 효과
    void GameOverEffect()
    {
        int childCount = this.transform.childCount;

        // 최근 20개의 블럭에 대해 효과 적용 (잔해는 제외)
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

    // (기존 블럭들 제거 및 초기 상태 복원)
    public void Restart()
    {
        int childCount = transform.childCount;

        // 모든 자식 오브젝트(블럭) 제거
        for (int i = 0; i < childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // 게임 오버 상태와 관련 변수들 초기화
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

        // 색상도 초기화
        prevColor = GetRandomColor();
        nextColor = GetRandomColor();

        // 초기 블럭 2개 생성
        Spawn_Block();
        Spawn_Block();
    }
}
