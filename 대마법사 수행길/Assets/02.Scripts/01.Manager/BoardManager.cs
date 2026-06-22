using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 텍스트를 위해 추가
using DG.Tweening; // DOTween을 위해 추가
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
    [Header("Water Skill Settings")]
    public Sprite waterFace;                 // 물 스킬 컷인 얼굴
    public TextMeshProUGUI waterBuffText;    // 시간 정지 텍스트
    public string waterBuffMessage = "시간 정지!"; // 에디터 수정용

    private int waterUses = 1;               // 물 스킬 사용 가능 횟수 (1회)
    private bool isWaterActive = false;      // 물 스킬 활성화 상태 확인

    [Header("Sound Effects (SFX)")]
    public AudioSource audioSource; // 소리를 내줄 스피커
    public AudioClip popSound;      // 뿌요 터질 때/합쳐질 때 소리
    public AudioClip skillSound;    // 스킬 컷인 발동 소리

    [Header("Skill Text Settings (에디터에서 수정 가능!)")]
    public string fireBuffMessage = "점수 2배 버프!";
    public string earthPopupMessage = "다음 보드 변경!";
    public string airBuffMessage = "연쇄 점수 2배 활성화!";

    [Header("Skill UI Effects")]
    public GameObject fullScreenSpeedLines; // 전체 화면 속도선
    public TextMeshProUGUI fireBuffText;    // 불 10초 타이머 텍스트
    public TextMeshProUGUI airBuffText;     // 바람 활성화 텍스트
    public TextMeshProUGUI skillPopupText;  // 땅 스킬 팝업 텍스트

    [Header("Game Over & Timer")]
    public TextMeshProUGUI timerText;       // 타이머 텍스트
    public GameObject gameOverPanel;        // 게임오버 패널
    public TextMeshProUGUI finalScoreText;  // 최종 점수 텍스트

    [Header("Skill Cut-In Animation")]
    public RectTransform cutInPanel;  // 하얀 선 역할을 할 마스크 패널
    public UnityEngine.UI.Image characterImage; // 캐릭터 일러스트 이미지

    public float timeLimit = 180f;          // 제한 시간 3분 (180초)
    public bool isGameOver = false;         // 게임오버 상태 확인

    private Vector3 popupOriginPos;

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

    private Vector2 cutInTargetSize;
    private Vector2 cutInOriginPos;

    [Header("Board Settings")]
    public int width = 7; //뿌요뿌요 가로 크기
    public int height = 12; //뿌요뿌요 세로 크기
    public float cellSize = 1.0f; //셀 크기

    public GameObject puyoPrefab;

    // 인덱스 0은 비워두고, 1:불, 2:물, 3:땅, 4:공기 이미지를 넣을 예정.
    public Sprite[] puyoSprites;

    // 속성별 캐릭터 얼굴 일러스트 (인스펙터에서 넣어주세요)
    public Sprite fireFace;
    public Sprite earthFace;
    public Sprite airFace;

    private GameObject[,] puyoObjects; //보드 상의 뿌요 오브젝트를 저장하는 2D 배열

    //게임 보드의 상태를 저장하는 2D 배열
    // 0: 빈 셀, 1: 빨간 뿌요, 2: 파란 뿌요, 3: 노란 뿌요, 4: 초록 뿌요 등
    private int[,] grid;

    private int[,] levelGrid;

    [Header("Effects")]
    public GameObject popEffectPrefab; // 새로 추가: 터질 때 생성할 파티클 이펙트

    public Transform gameOverTitle;  // "Game Over"라고 적힌 큰 글자
    public Transform restartButton;  // "다시 하기" 버튼

    // 스킬 중복 사용 방지용 변수
    private bool isSkillPlaying = false;

    void Awake()
    {
        grid = new int[width, height];
        puyoObjects = new GameObject[width, height];
        ClearBoard();

        comboText.gameObject.SetActive(false); // 시작할 때 콤보 텍스트 숨김
        UpdateScoreText();

        if (cutInPanel != null)
        {
            cutInTargetSize = cutInPanel.sizeDelta;
            cutInOriginPos = cutInPanel.anchoredPosition;
            cutInPanel.gameObject.SetActive(false); // 기억했으면 평소엔 꺼둡니다.
        }

        if (skillPopupText != null)
        {
            popupOriginPos = skillPopupText.transform.localPosition;
            skillPopupText.gameObject.SetActive(false);
        }
        if (fullScreenSpeedLines != null) fullScreenSpeedLines.SetActive(false);
        if (fireBuffText != null) fireBuffText.gameObject.SetActive(false);
        if (airBuffText != null) airBuffText.gameObject.SetActive(false);
        if (skillPopupText != null) skillPopupText.gameObject.SetActive(false);
        if (waterBuffText != null) waterBuffText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return; // 게임오버면 타이머 정지

        if (!isWaterActive)
        {
            timeLimit -= Time.deltaTime;
        }

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
        // 천장을 넘으면 게임오버!
        if (y >= height)
        {
            TriggerGameOver("보드가 꽉 찼습니다!");
            return;
        }

        grid[x, y] = type; // 보드에 타입(레벨) 기록

        Vector3 pos = GridToWorldPosition(x, y);
        GameObject newPuyo = Instantiate(puyoPrefab, pos, Quaternion.identity);

        SpriteRenderer sr = newPuyo.GetComponent<SpriteRenderer>();
        sr.color = Color.white;

        // puyoSprites 배열에서 타입(1, 2, 3...)에 맞는 이미지를 씌워줍니다.
        if (type >= 1 && type < puyoSprites.Length)
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
    public List<Vector2Int> FindConnectedPuyos(int startX, int startY, int type)
    {
        List<Vector2Int> connected = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, height];

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            connected.Add(current);

            foreach (Vector2Int dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                // 보드 안쪽이고, 천장(height) 안 넘고, 아직 방문 안 했고, 타입(색깔)이 같으면 연결!
                if (IsValidPosition(nx, ny) && ny < height && !visited[nx, ny] && grid[nx, ny] == type)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
        return connected;
    }

    public bool CheckAndDestroyMatches(int comboCount)
    {
        bool[,] hasMatched = new bool[width, height];
        bool matchFound = false;
        int destroyedPuyoCount = 0;
        int mergeScoreBonus = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != 0 && !hasMatched[x, y])
                {
                    int type = grid[x, y];
                    // 탐색할 때 level 관련은 빼고, 예전처럼 type만 넘깁니다.
                    List<Vector2Int> connected = FindConnectedPuyos(x, y, type);

                    if (connected.Count >= 4)
                    {
                        matchFound = true;
                        destroyedPuyoCount += connected.Count;
                        mergeScoreBonus += type * 2; // 상위 원소일수록 점수 폭발!

                        // 진화할 다음 단계 설정 (현재 타입 + 1)
                        int nextType = type + 1;

                        // 목표 위치 찾기 (가장 바닥에 있는 블록 기준)
                        Vector2Int mergeTarget = connected[0];
                        foreach (Vector2Int pos in connected)
                        {
                            if (pos.y < mergeTarget.y) mergeTarget = pos;
                        }

                        foreach (Vector2Int pos in connected)
                        {
                            hasMatched[pos.x, pos.y] = true;

                            if (pos == mergeTarget)
                            {
                                if (nextType >= puyoSprites.Length)
                                {
                                    grid[pos.x, pos.y] = 0;
                                    StartCoroutine(ShrinkAndDestroy(puyoObjects[pos.x, pos.y]));
                                    puyoObjects[pos.x, pos.y] = null;
                                    score += 10000;
                                    continue;
                                }

                                grid[pos.x, pos.y] = nextType;

                                GameObject mergedPuyo = puyoObjects[pos.x, pos.y];
                                SpriteRenderer sr = mergedPuyo.GetComponent<SpriteRenderer>();
                                sr.sprite = puyoSprites[nextType];

                                // 완벽 수정됨: Vector3.one 이 아니라, 프리팹 원본의 기본 크기로 복구합니다!
                                Vector3 originalScale = puyoPrefab.transform.localScale;
                                mergedPuyo.transform.localScale = originalScale;

                                // 통통 튕기는 애니메이션도 원본 크기 비율에 맞춰서 재생합니다.
                                mergedPuyo.transform.DOPunchScale(originalScale * 0.3f, 0.4f, 10, 1);

                                if (popEffectPrefab != null) Instantiate(popEffectPrefab, mergedPuyo.transform.position, Quaternion.identity);
                            }
                            else
                            {
                                // 나머지 3개는 화면에서 지워버림(재물로 바쳐짐)
                                grid[pos.x, pos.y] = 0;
                                StartCoroutine(ShrinkAndDestroy(puyoObjects[pos.x, pos.y]));
                                puyoObjects[pos.x, pos.y] = null;
                            }
                        }
                    }
                }
            }
        }

        if (matchFound)
        {
            if (audioSource != null && popSound != null)
            {
                audioSource.PlayOneShot(popSound);
            }

            CalculateScore(destroyedPuyoCount * mergeScoreBonus, comboCount);
            if (comboCount >= 2) ShowComboText(comboCount);
            return true;
        }
        return false;
    }

    // 새로 추가: 공중에 뜬 뿌요를 바닥까지 끌어내리는 중력 함수
    // 부드럽게 통통 떨어지는 중력 애니메이션 적용!
    public void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            // 아래에서 위로 스캔
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] != 0)
                {
                    int targetY = y;
                    while (targetY - 1 >= 0 && grid[x, targetY - 1] == 0) { targetY--; }

                    if (targetY != y)
                    {
                        grid[x, targetY] = grid[x, y];
                        grid[x, y] = 0;

                        GameObject puyo = puyoObjects[x, y];
                        puyoObjects[x, targetY] = puyo;
                        puyoObjects[x, y] = null;

                        if (puyo != null)
                        {
                            // 수정됨: 순간이동(position=...) 대신 부드럽게 떨어지는 DOMove 사용!
                            Vector3 targetPos = GridToWorldPosition(x, targetY);
                            // 0.3초 동안 목표 위치로 떨어지며, 바닥에 닿을 때 살짝 통통 튕깁니다 (OutBounce)
                            puyo.transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutBounce);
                        }
                        else
                        {
                            grid[x, targetY] = 0;
                        }
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
    public void UseFireSkill()
    {
        if (fireUses > 0 && !isFireActive && !isSkillPlaying)
        {
            ExecuteSkillWithCutIn(fireFace, () =>
            {
                fireUses--;
                StartCoroutine(FireSkillTimer());
                StartCoroutine(SpeedLinesTimer()); // 1.5초 속도선 켜기!
            });
        }
    }

    private IEnumerator FireSkillTimer()
    {
        isFireActive = true;
        fireBuffText.gameObject.SetActive(true);

        float timer = 10f;
        while (timer > 0)
        {
            // 에디터에서 적은 문구 뒤에 남은 시간만 딱 붙여줍니다!
            fireBuffText.text = $"{fireBuffMessage} ({Mathf.CeilToInt(timer)}초)";
            timer -= Time.deltaTime;
            yield return null;
        }

        isFireActive = false;
        fireBuffText.gameObject.SetActive(false);
    }

    // 2. 땅 스킬
    public void UseEarthSkill()
    {
        if (earthUses > 0 && !isSkillPlaying)
        {
            ExecuteSkillWithCutIn(earthFace, () =>
            {
                earthUses--;
                puyoController.RerollNextPuyo();

                // 에디터에서 적은 문구를 띄웁니다!
                ShowSkillPopup(earthPopupMessage);
                StartCoroutine(SpeedLinesTimer()); // 1.5초 속도선 켜기!
            });
        }
    }

    // 3. 공기(바람) 스킬
    public void UseAirSkill()
    {
        if (airUses > 0 && !isSkillPlaying)
        {
            ExecuteSkillWithCutIn(airFace, () =>
            {
                airUses--;
                baseComboBonus = 2.0f;

                airBuffText.gameObject.SetActive(true);
                airBuffText.text = airBuffMessage; // 에디터 문구 띄우기!
                airBuffText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1);

                StartCoroutine(SpeedLinesTimer()); // 1.5초 속도선 켜기!
            });
        }
    }


    public void UseWaterSkill()
    {
        // 사용 횟수가 남아있고, 이미 작동 중이 아니고, 다른 컷인이 없을 때
        if (waterUses > 0 && !isWaterActive && !isSkillPlaying)
        {
            ExecuteSkillWithCutIn(waterFace, () =>
            {
                waterUses--;
                StartCoroutine(WaterSkillTimer()); // 시간 정지 코루틴 시작
                StartCoroutine(SpeedLinesTimer()); // 1.5초 속도선 켜기
            });
        }
    }

    private IEnumerator WaterSkillTimer()
    {
        isWaterActive = true; // 타이머 정지 시작!
        waterBuffText.gameObject.SetActive(true);

        float timer = 10f; // 10초 유지
        while (timer > 0)
        {
            // 남은 시간을 텍스트에 표시
            waterBuffText.text = $"{waterBuffMessage} ({Mathf.CeilToInt(timer)}초)";
            timer -= Time.deltaTime;
            yield return null;
        }

        isWaterActive = false; // 타이머 다시 흐르기 시작!
        waterBuffText.gameObject.SetActive(false);
    }

    // 새로 추가: 순간 팝업 연출 함수
    private void ShowSkillPopup(string message)
    {
        skillPopupText.gameObject.SetActive(true);
        skillPopupText.text = message;

        // 강제로 정중앙(Vector3.zero)으로 보내던 것을 지우고, 기억해둔 원래 위치로 설정합니다!
        skillPopupText.transform.localPosition = popupOriginPos;
        skillPopupText.transform.localScale = Vector3.zero;
        skillPopupText.color = new Color(skillPopupText.color.r, skillPopupText.color.g, skillPopupText.color.b, 1f);

        Sequence seq = DOTween.Sequence();

        // 1. 0.3초 만에 띠용! 하고 커지면서 등장
        seq.Append(skillPopupText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));

        // 2.  원래 있던 위치에서 위로 150만큼 스르륵 올라가도록 수정!
        seq.Join(skillPopupText.transform.DOLocalMoveY(popupOriginPos.y + 150f, 1.5f).SetEase(Ease.OutQuad));

        // 3. 등장 1초 뒤부터 투명해지며 사라짐
        seq.Join(skillPopupText.DOFade(0f, 0.5f).SetDelay(1.0f));

        // 4. 애니메이션이 다 끝나면 오브젝트를 다시 꺼줌
        seq.OnComplete(() => skillPopupText.gameObject.SetActive(false));
    }

    // ==========================================
    //  페르소나 스타일 컷인 연출 코루틴 (카메라 쉐이크 적용!)
    // ==========================================
    private void ExecuteSkillWithCutIn(Sprite faceSprite, Action skillAction)
    {
        StartCoroutine(CutInRoutine(faceSprite, skillAction));
    }

    private IEnumerator CutInRoutine(Sprite faceSprite, Action skillAction)
    {
        isSkillPlaying = true;

        if (audioSource != null && skillSound != null)
        {
            audioSource.PlayOneShot(skillSound);
        }

        characterImage.sprite = faceSprite;
        cutInPanel.gameObject.SetActive(true);

        // 수정됨: 에디터에서 설정한 원래 '위치'를 그대로 가져옵니다.
        // 크기만 가로는 0, 세로는 15(얇은 선)로 찌그러뜨려서 시작합니다.
        cutInPanel.anchoredPosition = cutInOriginPos;
        cutInPanel.sizeDelta = new Vector2(0, 15);

        Sequence seq = DOTween.Sequence();

        // 수정됨: 에디터에서 설정한 원래 '가로 길이'만큼 쫙 가릅니다!
        seq.Append(cutInPanel.DOSizeDelta(new Vector2(cutInTargetSize.x, 15), 0.15f).SetEase(Ease.OutExpo));

        seq.AppendCallback(() => {
            Camera.main.transform.DOShakePosition(0.3f, 0.5f, 20);
        });

        // 수정됨: 에디터에서 설정한 원래 '세로 길이(높이)'까지 통통 튀며 확대됩니다!
        seq.Append(cutInPanel.DOSizeDelta(cutInTargetSize, 0.22f).SetEase(Ease.OutBack));

        characterImage.transform.localPosition = new Vector3(100, 0, 0);
        seq.Join(characterImage.transform.DOLocalMoveX(-50, 0.7f).SetEase(Ease.OutQuad));

        // 절정 타이밍: 스킬 효과가 발동하면서 '전체 화면 속도선'도 같이 터집니다!
        yield return new WaitForSeconds(0.4f);

        if (fullScreenSpeedLines != null) fullScreenSpeedLines.SetActive(true); // 전체 속도선 ON!

        skillAction?.Invoke(); // 실제 스킬 내용 실행

        yield return new WaitForSeconds(0.6f);

        cutInPanel.DOAnchorPosX(cutInOriginPos.x - 2500, 0.2f).SetEase(Ease.InExpo);

        yield return new WaitForSeconds(0.25f);

        cutInPanel.gameObject.SetActive(false);
        isSkillPlaying = false;

        // 스킬 연출이 완전히 끝나고 1초 정도 더 속도감을 준 뒤에 전체 속도선을 끕니다.
        yield return new WaitForSeconds(1.0f);
        if (fullScreenSpeedLines != null) fullScreenSpeedLines.SetActive(false); // 전체 속도선 OFF!
    }

    // 1.5초 속도선 전용 함수
    private IEnumerator SpeedLinesTimer()
    {
        if (fullScreenSpeedLines != null)
        {
            fullScreenSpeedLines.SetActive(true); // 속도선 ON
            yield return new WaitForSeconds(1.5f); // 딱 1.5초만 대기
            fullScreenSpeedLines.SetActive(false); // 속도선 OFF
        }
    }
}