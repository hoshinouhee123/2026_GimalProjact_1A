using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
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

    void Awake()
    {
        //1. 게임 시작 시 보드 초기화
        grid = new int[width, height];
        puyoObjects = new GameObject[width, height];    //뿌요 오브젝트 배열 초기화
        ClearBoard();
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
        // 수정됨: 블록이 화면 꼭대기(배열의 끝)를 넘어가면 에러 대신 게임 오버 로그를 띄웁니다.
        if (y >= height)
        {
            Debug.LogWarning("게임 오버! 보드가 꽉 찼습니다.");
            // 원래는 여기서 게임오버 팝업을 띄우거나 게임을 멈춰야 합니다.
            // 일단 에러가 안 나도록 return 으로 빠져나갑니다.
            return;
        }

        grid[x, y] = type;

        Vector3 pos = GridToWorldPosition(x, y);
        GameObject newPuyo = Instantiate(puyoPrefab, pos, Quaternion.identity);

        SpriteRenderer sr = newPuyo.GetComponent<SpriteRenderer>();
        sr.color = Color.white; // 색깔이 섞이지 않게 기본 흰색으로.

        // 배열에서 속성 번호(type)에 맞는 이미지를 꺼내서 적용
        if (type >= 1 && type <= 4)
        {
            sr.sprite = puyoSprites[type];
        }

        puyoObjects[x, y] = newPuyo;
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

    public bool CheckAndDestroyMatches()
    {
        bool[,] hasMatched = new bool[width, height]; // 터질 예정인 뿌요들 체크
        bool matchFound = false;

        // 보드 전체를 스캔합니다.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 빈 칸이 아니고, 아직 검사 안 한 뿌요라면
                if (grid[x, y] != 0 && !hasMatched[x, y])
                {
                    int type = grid[x, y];
                    List<Vector2Int> connected = FindConnectedPuyos(x, y, type);

                    // 4개 이상 연결되었는가 확인
                    if (connected.Count >= 4)
                    {
                        matchFound = true;

                        // 5개 이상 연결되면 스킬 발동
                        if (connected.Count >= 5)
                        {
                            Debug.Log($"[{type}] 속성 스킬 발동!! (연결된 개수: {connected.Count}개)");
                            // 나중에 여기에 이펙트나 몬스터 공격 코드를 넣으면 됨
                        }

                        // 터질 목록에 등록
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
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (hasMatched[x, y])
                    {
                        grid[x, y] = 0;

                        // 수정됨: 즉시 파괴하지 않고 부드럽게 줄어드는 애니메이션 코루틴 실행
                        GameObject puyoToDestroy = puyoObjects[x, y];
                        Vector3 pos = puyoToDestroy.transform.position; // 터지는 위치 저장

                        // 1. 파티클 이펙트 생성
                        if (popEffectPrefab != null)
                        {
                            Instantiate(popEffectPrefab, pos, Quaternion.identity);
                        }

                        // 2. 크기가 줄어들며 파괴되는 코루틴 실행
                        StartCoroutine(ShrinkAndDestroy(puyoToDestroy));

                        puyoObjects[x, y] = null; // 배열에서는 즉시 비워줌
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
}