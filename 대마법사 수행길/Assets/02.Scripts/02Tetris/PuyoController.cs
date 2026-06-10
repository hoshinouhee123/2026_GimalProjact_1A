using UnityEngine;
using System.Collections;


public class PuyoController : MonoBehaviour
{
    [Header("References")]
    public BoardManager boardManager;   //보드 매니저 참조
    public GameObject mainPuyoObj;      // 화면에 보이는 기준 뿌요 오브젝트
    public GameObject subPuyoObj;       // 화면에 보이는 회전하는 뿌요 오브젝트   


    [Header("Settings")]
    public float fallSpeed = 1.0f;      //뿌요가 떨어지는 속도(1초에 한 칸씩 떨어짐)


    // --- 추가된 속성 데이터 ---
    // 0: 빈칸, 1: 불, 2: 물, 3: 땅, 4: 공기
    private int mainType;
    private int subType;


    private Vector2Int mainPuyoPos;     //기준 뿌요의 보드 상 위치(x, y)
    private int rotationState = 0;             //회전 상태(0:위, 1:오른쪽, 2:아래, 3:왼쪽)
    private float fallTimer = 0.0f;              //뿌요가 떨어지는 타이머

    private bool canPlay = true;    // 콤보가 터지고 떨어지는 동안 플레이어가 조작하지 못하게 막는 변수


    void Start()
    {
        SpawnNewPuyo(); //게임 시작 시 새로운 뿌요 생성
    }

    void Update()
    {
        // 콤보 연출 중일 때는 키보드 입력이나 자동 낙하를 무시
        if (!canPlay) return;

        //1.키보드 입력 처리
        if (Input.GetKeyDown(KeyCode.A)) TryMove(-1, 0);        //왼쪽
        if (Input.GetKeyDown(KeyCode.D)) TryMove(1, 0);        //오른쪽
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1);        //아래로 빠르게

        //오른쪽 화살표 키로 시계 방향 회전
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryRotateClockwise();
        }

        // 왼쪽 화살표 키로 반시계 방향 회전
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryRotateCounterClockwise();
        }

        //2. 시간이 지나면 자동으로 한 칸 씩 떨어짐
        fallTimer += Time.deltaTime;    //타이머에 경과 시간 추가
        if (fallTimer >= fallSpeed)     //1초가 지나면
        {
            TryMove(0, -1); //아래로 한 칸 이동 시도
            fallTimer = 0.0f; //타이머 초기화
        }
    }

    public void SpawnNewPuyo()
    {
        mainPuyoPos = new Vector2Int(boardManager.width / 2, boardManager.height - 1); //보드 중앙 맨 위에 뿌요 생성
        rotationState = 0; //sub 뿌요는 처음에 위쪽에 위치

        //1부터 4까지(5는 포함 안됨) 랜덤한 숫자를 뽑아 속성 부여
        mainType = Random.Range(1, 5);
        subType = Random.Range(1, 5);

        // 새로 추가: 숨겨뒀던 원본 뿌요들을 다시 화면에 보이게 켭니다!
        mainPuyoObj.SetActive(true);
        subPuyoObj.SetActive(true);

        UpdateVisuals(); 
    }


    // 현재 회전 상태에 따른 sub 뿌요의 위치 계산
    private Vector2Int GetSubPuyoPos(Vector2Int mPos, int rot)
    {
        switch (rot)
        {
            case 0: return mPos + Vector2Int.up;    // (0,1) 위
            case 1: return mPos + Vector2Int.right; // (1,0) 오른쪽
            case 2: return mPos + Vector2Int.down;  // (0,-1) 아래
            case 3: return mPos + Vector2Int.left;  // (-1,0) 왼쪽
            default: return mPos + Vector2Int.up;   //기본적으로 위쪽

        }
    }

    private void TryMove(int dx, int dy)
    {
        Vector2Int nextMainPos = mainPuyoPos + new Vector2Int(dx, dy); //이동하려는 기준 뿌요의 다음 위치 계산
        Vector2Int nextSubPos = GetSubPuyoPos(nextMainPos, rotationState); //이동하려는 sub 뿌요의 다음 위치 계산

        //다음 위치가 보드 안에 있고, 두 뿌요 모두 빈 칸인지 확인
        if (boardManager.IsEmpty(nextMainPos.x, nextMainPos.y) && boardManager.IsEmpty(nextSubPos.x, nextSubPos.y))
        {
            mainPuyoPos = nextMainPos; //이동이 가능하면 기준 뿌요 위치 업데이트
            UpdateVisuals(); //화면 업데이트
        }
        else if (dy == -1) //아래로 이동이 불가능한 경우(뿌요가 바닥이나 다른 뿌요에 닿은 경우)
        {
            LockPuyos(); //뿌요를 보드에 고정

            // 새로 추가: 보드에 복제본을 심었으니, 플레이어가 조종하던 원본은 화면에서 잠시 숨깁니다!
            mainPuyoObj.SetActive(false);
            subPuyoObj.SetActive(false);

            StartCoroutine(ProcessMatchesRoutine());    //연쇄 콤보를 관리하는 코루틴을 실행
        }
    }

    private void LockPuyos()
    {
        Vector2Int subPos = GetSubPuyoPos(mainPuyoPos, rotationState); //현재 회전 상태에서 sub 뿌요의 위치 계산

        bool mainIsLower = mainPuyoPos.y <= subPos.y; //기준 뿌요가 sub 뿌요보다 아래에 있는지 확인

        if (mainIsLower)
        {
            DropAndLock(mainPuyoPos.x, mainPuyoPos.y, mainType);
            DropAndLock(subPos.x, subPos.y, subType);
        }
        else
        {
            DropAndLock(subPos.x, subPos.y, subType);
            DropAndLock(mainPuyoPos.x, mainPuyoPos.y, mainType);
        }
    }

    // 뿌요를 떨어뜨리고 보드에 고정하는 함수
    private void DropAndLock(int x, int startY, int type)
    {
        int targetY = startY;

        // 자신의 바로 아래(targetY - 1)가 보드 범위 안이고 빈칸(0)이면 계속 내려감
        while (targetY - 1 >= 0 && boardManager.IsEmpty(x, targetY - 1))
        {
            targetY--;
        }

        // 바닥을 찾았으니 보드에 기록
        boardManager.PlacePuyo(x, targetY, type);
    }

    // 시계 방향 회전 함수
    private void TryRotateClockwise()
    {
        //0 -> 1 -> 2 -> 3 -> 0 (시계 방향 회전)
        int NextRot = (rotationState + 1) % 4; //다음 회전 상태 계산
        Vector2Int nextSubPos = GetSubPuyoPos(mainPuyoPos, NextRot); //다음 회전 상태에서 sub 뿌요의 위치 계산

        if (boardManager.IsEmpty(nextSubPos.x, nextSubPos.y))
        {
            rotationState = NextRot; //회전이 가능하면 회전 상태 업데이트
            UpdateVisuals(); //화면 업데이트
        }
    }

    // 반시계 방향 회전 함수 추가
    private void TryRotateCounterClockwise()
    {
        // 0 -> 3 -> 2 -> 1 -> 0 (반시계 방향 회전)
        // C#에서 음수 나머지 연산을 방지하기 위해 (상태 - 1 + 4) % 4 를 함.
        int nextRot = (rotationState + 3) % 4;
        Vector2Int nextSubPos = GetSubPuyoPos(mainPuyoPos, nextRot);

        if (boardManager.IsEmpty(nextSubPos.x, nextSubPos.y))
        {
            rotationState = nextRot; // 회전이 가능하면 회전 상태 업데이트
            UpdateVisuals(); // 화면 업데이트
        }
    }

    private void UpdateVisuals()
    {
        mainPuyoObj.transform.position = boardManager.GridToWorldPosition(mainPuyoPos.x, mainPuyoPos.y); //기준 뿌요 위치 업데이트
        Vector2Int subPuyoPos = GetSubPuyoPos(mainPuyoPos, rotationState); //현재 회전 상태에서 sub 뿌요의 위치 계산
        subPuyoObj.transform.position = boardManager.GridToWorldPosition(subPuyoPos.x, subPuyoPos.y); //sub 뿌요 위치 업데이트

        SetPuyoSprite(mainPuyoObj, mainType);
        SetPuyoSprite(subPuyoObj, subType);
    }

    private void SetPuyoSprite(GameObject puyo, int type)
    {
        SpriteRenderer sr = puyo.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.color = Color.white; // 색상 초기화

        // BoardManager에 저장해둔 이미지 배열을 가져다 씀.
        if (type >= 1 && type <= 4)
        {
            sr.sprite = boardManager.puyoSprites[type];
        }
    }

    private IEnumerator ProcessMatchesRoutine()
    {
        canPlay = false; // 진행되는 동안 플레이어 조작 금지

        // 무한 루프로 콤보가 끝날 때까지 반복.
        while (true)
        {
            // 1. 4개 이상 연결된 뿌요 터뜨리기
            bool matched = boardManager.CheckAndDestroyMatches();

            if (!matched)
            {
                // 터진 게 없다면 무한 루프를 빠져나감. (콤보 종료)
                break;
            }

            // 2. 터진 직후 바로 떨어지면 안 보이니 0.5초 잠깐 대기 
            yield return new WaitForSeconds(0.5f);

            // 3. 중간이 비었으니 공중에 뜬 뿌요들을 아래로 떨어뜨리기 
            boardManager.ApplyGravity();

            // 4. 떨어지고 나서 바로 터지지 않게 또 0.5초 대기 
            // (이후 다시 while문 처음으로 돌아가서 또 4개가 모였는지 검사 = 연쇄 작용)
            yield return new WaitForSeconds(0.5f);
        }

        // 모든 연쇄(콤보)가 끝나고 조용해지면
        SpawnNewPuyo(); // 맨 위에서 새 뿌요 스폰
        canPlay = true; // 다시 플레이어 조작 허용
    }
}
