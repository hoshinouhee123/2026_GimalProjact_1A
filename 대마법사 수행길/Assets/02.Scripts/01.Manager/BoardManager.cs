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
}
