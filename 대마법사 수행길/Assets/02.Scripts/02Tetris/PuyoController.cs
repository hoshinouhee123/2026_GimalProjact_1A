using UnityEngine;

public class PuyoController : MonoBehaviour
{
    [Header("References")]
    public BoardManager boardManager;   //보드 매니저 참조
    public GameObject mainPuyoObj;      // 화면에 보이는 기준 뿌요 오브젝트
    public GameObject subPuyoObj;       // 화면에 보이는 회전하는 뿌요 오브젝트   


    [Header("Settings")]
    public float fallSpeed = 1.0f;      //뿌요가 떨어지는 속도(1초에 한 칸씩 떨어짐)

    //
    private Vector2Int mainPuyoPos;     //기준 뿌요의 보드 상 위치(x, y)
    private int rotationState = 0;             //회전 상태(0:위, 1:오른쪽, 2:아래, 3:왼쪽)
    private float fallTimer = 0.0f;              //뿌요가 떨어지는 타이머


    void Start()
    {
        SpawnNewPuyo(); //게임 시작 시 새로운 뿌요 생성
    }

    void Update()
    {
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
            Debug.Log("뿌요가 바닥이나 다른 뿌요에 닿았습니다. 고정 처리 필요!"); //고정 처리 필요

            // 임시로 다시 맨 위에서 스폰되게 처리
            SpawnNewPuyo(); //새로운 뿌요 생성
        }
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
    }



}
