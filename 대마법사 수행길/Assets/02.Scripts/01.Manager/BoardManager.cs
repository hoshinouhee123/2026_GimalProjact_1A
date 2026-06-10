using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 텍스트를 위해 추가
using DG.Tweening; // DOTween을 위해 추가
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
    [Header("Game Over & Timer")]
    public TextMeshProUGUI timerText;       // 타이머 텍스트
    public GameObject gameOverPanel;        // 게임오버 패널
    public TextMeshProUGUI finalScoreText;  // 최종 점수 텍스트

    public float timeLimit = 180f;          // 제한 시간 3분 (180초)
    public bool isGameOver = false;         // 게임오버 상태 확인

    [Header("UI & Score")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public int score = 0;

    [Header("Skill Settings")]
    public PuyoController puyoController; // Земля(Earth) 스킬을 위해 조종사 연결 필요

    // 남은 횟수
    private int fireUses = 1;
    private int earthUses = 3;
    private int airUses = 1;

    // 스킬 상태 변수
    private bool isFireActive = false;
    private float baseComboBonus = 1.5f; // 기본 연쇄 보너스 1.5배 (공기 스킬 쓰면 증가)

    [Header("Board Settings")]
    public int width = 7; //뿌요뿌요 가로 크기
    public int height = 12; //뿌요뿌요 세로 크기
    public float cellSize = 1.0f; //셀 크기

    public GameObject puyoPrefab;

    // 인덱스 0은 비워두고, 1:불, 2:물, 3:땅, 4:공기 이미지를 넣을 예정.
    public Sprite[] puyoSprites;

    private GameObject[,] puyoObjects; //보드 상의 뿌요 오브젝트를 저장하는 2D 배열

    //게임 보드의 상태를 저장하는 2D 배열
    // 0: 빈 셀, 1: 빨간 뿌요, 2: 파란 뿌요, 3: 노란 뿌요, 4: 초록 뿌요 등
    private int[,] grid;

    [Header("Effects")]
    public GameObject popEffectPrefab; // 새로 추가: 터질 때 생성할 파티클 이펙트

    public Transform gameOverTitle;  // "Game Over"라고 적힌 큰 글자
    public Transform restartButton;  // "다시 하기" 버튼

    void Awake()
    {
        grid = new int[width, height];
        puyoObjects = new GameObject[width, height];
        ClearBoard();

        comboText.gameObject.SetActive(false); // 시작할 때 콤보 텍스트 숨김
        UpdateScoreText();

        gameOverPanel.SetActive(false); // 시작할 때 게임오버 패널 끄기
    }

    void Update()
    {
        if (isGameOver) return; // 게임오버면 타이머 정지

        timeLimit -= Time.deltaTime; // 시간 감소

        if (timeLimit <= 0)
        {
            timeLimit = 0;
            TriggerGameOver("Time Over!"); // 시간이 다 되면 게임오버!
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timeLimit / 60);
        int seconds = Mathf.FloorToInt(timeLimit % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 보드를 전부 빈칸(0)으로 초기화하는 함수
    private void ClearBoard()
    {
        //중첩 반복문을 사용하여 2D 배열의 모든 셀을 0으로 초기화
        for (int x = 0; x < width; x++)     //가로 방향으로 반복
        {
            for (int y = 0; y < height; y++)        //세로 방향으로 반복
            {
                grid[x, y] = 0; //모든 셀을 빈 상태로 초기화
            }
        }
    }

    // 2. 특정 좌표가 보드 판 안에 있는지 확인하는 함수 (벽이나 바닥 뚫기 방지)
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }

    //3.해당 칸이 비어있는지(0인지) 확인하는 함수 (다른 뿌요와 겹치기 방지)
    public bool IsEmpty(int x, int y)
    {
        if (!IsValidPosition(x, y)) return false; // 벽이나 바닥 밖이면 거짓

        // 천장(height)보다 높은 곳은 배열 검사를 하지 않고 무조건 빈칸으로 취급
        // 이걸 안 쓰면 grid[x, 12]를 검사하려다가 에러
        if (y >= height) return true;

        return grid[x, y] == 0;
    }

    // 4. 배열의 논리적 좌표(x, y)를 유니티 화면에 실제 좌표(Vector3)로 변환하는 함수
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float posX = x * cellSize;      //x 좌표를 셀 크기만큼 곱하여 실제 위치 계산
        float posY = y * cellSize;      //y 좌표를 셀 크기만큼 곱하여 실제 위치 계산
        return new Vector3(posX, posY, 0) + transform.position;     //보드 매니저의 위치를 기준으로 실제 좌표 계산
    }

    public void PlacePuyo(int x, int y, int type)
    {
        // 수정됨: 블록이 천장을 넘어가면 게임오버 발동
        if (y >= height)
        {
            TriggerGameOver("보드가 꽉 찼습니다!");
            return;
        }

        grid[x, y] = type;

        Vector3 pos = GridToWorldPosition(x, y);
        GameObject newPuyo = Instantiate(puyoPrefab, pos, Quaternion.identity);

        SpriteRenderer sr = newPuyo.GetComponent<SpriteRenderer>();
        sr.color = Color.white;

        if (type >= 1 && type <= 4)
        {
            sr.sprite = puyoSprites[type];
        }

        puyoObjects[x, y] = newPuyo;
    }

    public void TriggerGameOver(string reason)
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("게임 오버 원인: " + reason);

        // 진행 중이던 모든 DOTween 강제 종료 (안전 장치)
        DOTween.KillAll();

        // 1. 일단 패널 켜기 (아직 글자는 안 보임)
        gameOverPanel.SetActive(true);
        finalScoreText.text = "최종 점수: " + score + " 점";

        // 2. 애니메이션 시작 전 초기 상태 세팅
        // 타이틀을 화면 한참 위(+800)로 올려둡니다. (원래 위치를 기억해둠)
        float originalY = gameOverTitle.localPosition.y;
        gameOverTitle.localPosition = new Vector3(gameOverTitle.localPosition.x, originalY + 800f, 0);

        // 점수와 버튼은 크기를 0으로 만들어서 숨깁니다.
        finalScoreText.transform.localScale = Vector3.zero;
        restartButton.localScale = Vector3.zero;

        // 3. DOTween 시퀀스(연속 애니메이션) 만들기
        Sequence gameOverSeq = DOTween.Sequence();

        // [첫 번째 연출]: 타이틀이 원래 위치(originalY)로 1.5초 동안 통통 튀며(OutBounce) 떨어집니다!
        gameOverSeq.Append(gameOverTitle.DOLocalMoveY(originalY, 1.5f).SetEase(Ease.OutBounce));

        // [두 번째 연출]: 타이틀이 다 떨어지면, 점수와 버튼이 0.4초 동안 뿅!(OutBack) 하고 커지며 나타납니다.
        gameOverSeq.Append(finalScoreText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
        gameOverSeq.Join(restartButton.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack)); // Join은 앞의 연출과 동시에 실행하라는 뜻
    }

    public void RestartGame()
    {
        // 현재 씬을 다시 로드합니다. (모든 게 초기화됨)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 5.유니티에서 그리드 선을 볼 수 있도록 하는 함수 (디버그용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray; //그리드 선 색상 설정

        //가로 선 그리기
        for (int x = 0; x <= width; x++) //가로 선 그리기
        {
            Vector3 start = GridToWorldPosition(x, 0); //시작점 계산
            Vector3 end = GridToWorldPosition(x, height); //끝점 계산
            Gizmos.DrawLine(start, end); //선 그리기
        }

        //세로 선 그리기
        for (int y = 0; y <= height; y++)
        {
            Vector3 start = GridToWorldPosition(0, y);      //시작점 계산
            Vector3 end = GridToWorldPosition(width, y);    //끝점 계산
            Gizmos.DrawLine(start, end);    //선 그리기
        }
    }

    // 6. 특정 좌표에서 같은 색상의 뿌요가 연결된 모든 좌표를 찾는 함수 (연쇄 판정에 사용)
    public List<Vector2Int> FindConnectedPuyos(int startX, int startY, int type)    //BFS 알고리즘을 사용하여 같은 색상의 뿌요가 연결된 모든 좌표를 찾는 함수
    {
        List<Vector2Int> connected = new List<Vector2Int>();        //연결된 뿌요의 좌표를 저장할 리스트
        Queue<Vector2Int> queue = new Queue<Vector2Int>();          // 방문 여부를 저장하는 2D 배열
        bool[,] visited = new bool[width, height];                  // 방문 여부를 저장하는 2D 배열 초기화

        // 시작점 설정
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        // 상, 하, 좌, 우 방향
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0) //  큐가 빌 때까지 반복
        {
            Vector2Int current = queue.Dequeue();   // 큐에서 현재 좌표를 꺼냄
            connected.Add(current);     //  현재 좌표를 연결된 리스트에 추가

            // 4방향 검사
            foreach (Vector2Int dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                // 보드 안이고, 방문한 적 없으며, 색상(type)이 같으면 큐에 추가
                if (IsValidPosition(nx, ny) && !visited[nx, ny] && grid[nx, ny] == type)
                {
                    visited[nx, ny] = true;     // 방문 표시
                    queue.Enqueue(new Vector2Int(nx, ny));      // 큐에 새로운 좌표 추가
                }
            }
        }
        return connected;       // 연결된 뿌요의 좌표 리스트 반환
    }

    // 수정됨: 매개변수로 콤보(comboCount)를 받아서 점수를 계산합니다.
    public bool CheckAndDestroyMatches(int comboCount)
    {
        bool[,] hasMatched = new bool[width, height];
        bool matchFound = false;
        int destroyedPuyoCount = 0; // 터진 뿌요 개수 세기

        // (기존 Flood Fill 탐색 로직 동일하게 유지...)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != 0 && !hasMatched[x, y])
                {
                    int type = grid[x, y];
                    List<Vector2Int> connected = FindConnectedPuyos(x, y, type);

                    if (connected.Count >= 4)
                    {
                        matchFound = true;
                        destroyedPuyoCount += connected.Count; // 개수 누적

                        foreach (Vector2Int pos in connected)
                        {
                            hasMatched[pos.x, pos.y] = true;
                        }
                    }
                }
            }
        }

        if (matchFound)
        {
            //  1. 점수 계산 로직
            CalculateScore(destroyedPuyoCount, comboCount);

            //  2. 콤보 텍스트 DOTween 연출 (2연쇄 이상일 때만)
            if (comboCount >= 2)
            {
                ShowComboText(comboCount);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (hasMatched[x, y])
                    {
                        grid[x, y] = 0;

                        GameObject puyoToDestroy = puyoObjects[x, y];
                        Vector3 pos = puyoToDestroy.transform.position;

                        if (popEffectPrefab != null) Instantiate(popEffectPrefab, pos, Quaternion.identity);

                        StartCoroutine(ShrinkAndDestroy(puyoToDestroy));
                        puyoObjects[x, y] = null;
                    }
                }
            }
            return true;
        }
        return false;
    }

    // 새로 추가: 공중에 뜬 뿌요를 바닥까지 끌어내리는 중력 함수
    public void ApplyGravity()
    {
        // 각 세로줄(열)마다 검사합니다.
        for (int x = 0; x < width; x++)
        {
            // 맨 밑바닥(0)은 더 떨어질 곳이 없으니 1층부터 꼭대기까지 검사합니다.
            for (int y = 1; y < height; y++)
            {
                // 해당 칸에 뿌요가 존재한다면
                if (grid[x, y] != 0)
                {
                    int targetY = y;

                    // 자신의 바로 아래 칸이 범위 안이고, 빈칸(0)이라면 계속 한 칸씩 목표치를 내립니다.
                    while (targetY - 1 >= 0 && grid[x, targetY - 1] == 0)
                    {
                        targetY--;
                    }

                    // 만약 원래 위치(y)보다 더 아래(targetY)로 떨어질 수 있다면
                    if (targetY != y)
                    {
                        // 1. 보드 배열 데이터 이동
                        grid[x, targetY] = grid[x, y];
                        grid[x, y] = 0; // 원래 있던 자리는 빈칸으로

                        // 2. 화면의 오브젝트 이동
                        GameObject puyo = puyoObjects[x, y];
                        puyoObjects[x, targetY] = puyo;
                        puyoObjects[x, y] = null;

                        // 실제 유니티 화면에서의 위치를 뚝 떨어뜨림
                        puyo.transform.position = GridToWorldPosition(x, targetY);
                    }
                }
            }
        }
    }

    // 새로 추가: 뿌요가 스르륵 작아지면서 사라지게 만드는 함수
    private IEnumerator ShrinkAndDestroy(GameObject puyo)
    {
        float duration = 0.25f; // 0.25초 동안 작아짐
        float time = 0f;
        Vector3 startScale = puyo.transform.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // 크기를 원래 크기에서 0으로 서서히 줄입니다.
            puyo.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null; // 한 프레임 대기
        }

        // 완전히 작아지면 화면에서 삭제
        Destroy(puyo);
    }

    private void CalculateScore(int count, int combo)
    {
        // 1. 기본 점수 (뿌요 1개당 100점)
        float currentScore = count * 100f;

        // 2. 연쇄 보너스 (1.5배수 적용) -> 1연쇄는 1배, 2연쇄는 1.5배, 3연쇄는 2.25배...
        float comboMultiplier = Mathf.Pow(baseComboBonus, combo - 1);
        currentScore *= comboMultiplier;

        // 3. 불 스킬(점수 2배) 적용
        if (isFireActive) currentScore *= 2f;

        // 점수 합산 및 UI 업데이트
        score += Mathf.RoundToInt(currentScore);
        UpdateScoreText();

        // 점수가 오를 때 텍스트가 통통 튀는 DOTween 연출!
        scoreText.transform.DOKill(); // 기존 애니메이션 취소
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 1);
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    private void ShowComboText(int combo)
    {
        comboText.gameObject.SetActive(true);
        comboText.text = combo + " 연쇄!!";

        // DOTween Sequence로 나타났다 사라지는 쫀득한 애니메이션
        Sequence seq = DOTween.Sequence();
        comboText.transform.localScale = Vector3.zero;

        seq.Append(comboText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack)); // 뿅! 나타남
        seq.AppendInterval(0.5f); // 0.5초 대기
        seq.Append(comboText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)); // 스르륵 사라짐
    }

    // ==========================================
    // 스킬 발동 함수들 (버튼에 연결할 것들)
    // ==========================================

    // 1. 불 스킬 (10초간 점수 2배)
    public void UseFireSkill()
    {
        if (fireUses > 0 && !isFireActive)
        {
            fireUses--;
            Debug.Log(" 불 스킬 발동! 10초간 점수 2배!");
            StartCoroutine(FireSkillTimer());
        }
    }

    private IEnumerator FireSkillTimer()
    {
        isFireActive = true;
        yield return new WaitForSeconds(10f);
        isFireActive = false;
        Debug.Log(" 불 스킬 종료.");
    }

    // 2. 땅 스킬 (다음 블록 색깔 랜덤 변화)
    public void UseEarthSkill()
    {
        if (earthUses > 0)
        {
            earthUses--;
            Debug.Log($" 땅 스킬 발동! 다음 블록 리롤 (남은횟수: {earthUses})");
            puyoController.RerollNextPuyo(); // 조종사에게 명령!
        }
    }

    // 3. 공기 스킬 (연쇄 보너스 증가 1.5배 -> 2.0배)
    public void UseAirSkill()
    {
        if (airUses > 0)
        {
            airUses--;
            Debug.Log(" 공기 스킬 발동! 이제부터 연쇄 보너스 대폭 증가!");
            baseComboBonus = 2.0f;
        }
    }
}