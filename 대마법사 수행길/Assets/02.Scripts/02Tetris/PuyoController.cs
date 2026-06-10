using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PuyoController : MonoBehaviour
{
    [Header("References")]
    public BoardManager boardManager;
    public GameObject mainPuyoObj;
    public GameObject subPuyoObj;

    // 1. 다음 뿌요 미리보기용 오브젝트 추가
    [Header("Next Puyo Preview")]
    public GameObject nextMainPreviewObj;
    public GameObject nextSubPreviewObj;

    [Header("Settings")]
    public float fallSpeed = 1.0f;

    private int mainType;
    private int subType;

    // 2. 다음 스폰될 뿌요 속성 저장 변수
    private int nextMainType;
    private int nextSubType;

    private Vector2Int mainPuyoPos;
    private int rotationState = 0;
    private float fallTimer = 0.0f;
    private bool canPlay = true;

    void Start()
    {
        // 3. 게임 시작 시 '다음 뿌요'를 먼저 하나 뽑아둡니다.
        nextMainType = Random.Range(1, 5);
        nextSubType = Random.Range(1, 5);

        SpawnNewPuyo();
    }

    void Update()
    {
        if (!canPlay) return;

        // 1. 이동 키: 오직 A, D, 아래 방향키만 사용합니다! (여기서 화살표를 제거했습니다)
        if (Input.GetKeyDown(KeyCode.A)) TryMove(-1, 0);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(1, 0);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1);

        // 2. 회전 키: 오직 좌/우 화살표만 사용합니다!
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryRotateClockwise();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryRotateCounterClockwise();

        // 3. 시간 경과에 따른 자동 낙하
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallSpeed)
        {
            TryMove(0, -1);
            fallTimer = 0.0f;
        }
    }

    public void RerollNextPuyo()
    {
        nextMainType = Random.Range(1, 5);
        nextSubType = Random.Range(1, 5);
        UpdatePreviewVisuals();

        // 텍스트뿐만 아니라 뿌요 이미지도 쫀득하게 튕기는 DOTween 효과!
        if (nextMainPreviewObj != null) nextMainPreviewObj.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        if (nextSubPreviewObj != null) nextSubPreviewObj.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
    }

    public void SpawnNewPuyo()
    {
        mainPuyoPos = new Vector2Int(boardManager.width / 2, boardManager.height - 1);
        rotationState = 0;

        // 4. 미리 뽑아둔 '다음 뿌요'를 '현재 뿌요'로 가져옵니다.
        mainType = nextMainType;
        subType = nextSubType;

        // 5. 새로운 '다음 뿌요'를 미리 뽑아둡니다.
        nextMainType = Random.Range(1, 5);
        nextSubType = Random.Range(1, 5);

        mainPuyoObj.SetActive(true);
        subPuyoObj.SetActive(true);

        UpdateVisuals();
        UpdatePreviewVisuals(); // 미리보기 화면 업데이트
    }

    private Vector2Int GetSubPuyoPos(Vector2Int mPos, int rot)
    {
        switch (rot)
        {
            case 0: return mPos + Vector2Int.up;
            case 1: return mPos + Vector2Int.right;
            case 2: return mPos + Vector2Int.down;
            case 3: return mPos + Vector2Int.left;
            default: return mPos + Vector2Int.up;
        }
    }

    private void TryMove(int dx, int dy)
    {
        Vector2Int nextMainPos = mainPuyoPos + new Vector2Int(dx, dy);
        Vector2Int nextSubPos = GetSubPuyoPos(nextMainPos, rotationState);

        if (boardManager.IsEmpty(nextMainPos.x, nextMainPos.y) && boardManager.IsEmpty(nextSubPos.x, nextSubPos.y))
        {
            mainPuyoPos = nextMainPos;
            UpdateVisuals();
        }
        else if (dy == -1)
        {
            LockPuyos();

            mainPuyoObj.SetActive(false);
            subPuyoObj.SetActive(false);

            StartCoroutine(ProcessMatchesRoutine());
        }
    }

    private void LockPuyos()
    {
        Vector2Int subPos = GetSubPuyoPos(mainPuyoPos, rotationState);
        bool mainIsLower = mainPuyoPos.y <= subPos.y;

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

    private void DropAndLock(int x, int startY, int type)
    {
        int targetY = startY;
        while (targetY - 1 >= 0 && boardManager.IsEmpty(x, targetY - 1))
        {
            targetY--;
        }
        boardManager.PlacePuyo(x, targetY, type);
    }

    // 6. 시계 / 반시계 회전 함수 정리
    private void TryRotateClockwise()
    {
        int nextRot = (rotationState + 1) % 4;
        AttemptRotation(nextRot);
    }

    private void TryRotateCounterClockwise()
    {
        int nextRot = (rotationState + 3) % 4;
        AttemptRotation(nextRot);
    }

    // 7. 핵심: 벽 차기(Wall Kick) 로직이 포함된 회전 검사 함수
    private void AttemptRotation(int nextRot)
    {
        Vector2Int nextSubPos = GetSubPuyoPos(mainPuyoPos, nextRot);

        // 1순위: 제자리 회전이 가능한가?
        if (boardManager.IsEmpty(nextSubPos.x, nextSubPos.y))
        {
            rotationState = nextRot;
            UpdateVisuals();
            return; // 성공했으니 종료
        }

        // 2순위: 벽 차기 (Wall Kick)
        // 제자리 회전이 막혔다면, Main 뿌요를 상/하/좌/우로 1칸 밀었을 때 회전이 되는지 검사합니다.
        Vector2Int[] kickOffsets = { Vector2Int.left, Vector2Int.right, Vector2Int.up };

        foreach (Vector2Int kick in kickOffsets)
        {
            Vector2Int kickedMainPos = mainPuyoPos + kick;
            Vector2Int kickedSubPos = GetSubPuyoPos(kickedMainPos, nextRot);

            // 밀어낸 자리에 Main과 Sub가 모두 들어갈 수 있다면!
            if (boardManager.IsEmpty(kickedMainPos.x, kickedMainPos.y) &&
                boardManager.IsEmpty(kickedSubPos.x, kickedSubPos.y))
            {
                // 위치를 밀어낸 곳으로 변경하고 회전 적용
                mainPuyoPos = kickedMainPos;
                rotationState = nextRot;
                UpdateVisuals();
                return; // 성공했으니 종료
            }
        }
    }

    // 8. 화면 업데이트 (미리보기 포함)
    private void UpdateVisuals()
    {
        mainPuyoObj.transform.position = boardManager.GridToWorldPosition(mainPuyoPos.x, mainPuyoPos.y);
        Vector2Int subPuyoPos = GetSubPuyoPos(mainPuyoPos, rotationState);
        subPuyoObj.transform.position = boardManager.GridToWorldPosition(subPuyoPos.x, subPuyoPos.y);

        SetPuyoSprite(mainPuyoObj, mainType);
        SetPuyoSprite(subPuyoObj, subType);
    }

    private void UpdatePreviewVisuals()
    {
        // 미리보기 오브젝트가 연결되어 있다면 색깔/이미지를 바꿔줍니다.
        if (nextMainPreviewObj != null && nextSubPreviewObj != null)
        {
            SetPuyoSprite(nextMainPreviewObj, nextMainType);
            SetPuyoSprite(nextSubPreviewObj, nextSubType);
        }
    }

    private void SetPuyoSprite(GameObject puyo, int type)
    {
        SpriteRenderer sr = puyo.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.color = Color.white;
        if (type >= 1 && type <= 4)
        {
            sr.sprite = boardManager.puyoSprites[type];
        }
    }

    private IEnumerator ProcessMatchesRoutine()
    {
        canPlay = false;
        int comboCount = 1; // 콤보 카운터 시작!

        while (true)
        {
            // 보드매니저에게 현재 콤보 횟수를 넘겨줍니다.
            bool matched = boardManager.CheckAndDestroyMatches(comboCount);

            if (!matched) break;

            comboCount++; // 터졌다면 콤보 +1 증가!

            yield return new WaitForSeconds(0.3f);
            boardManager.ApplyGravity();
            yield return new WaitForSeconds(0.4f);
        }

        SpawnNewPuyo();
        canPlay = true;
    }
}