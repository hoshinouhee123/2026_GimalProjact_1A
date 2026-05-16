using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 7; //뿌요뿌요 가로 크기
    public int height = 12; //뿌요뿌요 세로 크기
    public float cellSize = 1.0f; //셀 크기

    //게임 보드의 상태를 저장하는 2D 배열
    // 0: 빈 셀, 1: 빨간 뿌요, 2: 파란 뿌요, 3: 노란 뿌요, 4: 초록 뿌요 등
    private int[,] grid; 

    void Awake()
    {
        //1. 게임 시작 시 보드 초기화
        grid = new int[width, height];
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
        // 가로가 0~6 사이이고, 세로가 0 이상이면 참(true)
        // 위로 올라가는것(y < heghit)도 막아줌
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //3.해당 칸이 비어있는지(0인지) 확인하는 함수 (다른 뿌요와 겹치기 방지)
    public bool IsEmpty(int x, int y)
    {
        if (!IsValidPosition(x, y)) return false; //유효하지 않은 위치는 빈 칸이 아님
        return grid[x, y] == 0; //해당 칸이 0이면 빈 칸
    }

    // 4. 배열의 논리적 좌표(x, y)를 유니티 화면에 실제 좌표(Vector3)로 변환하는 함수
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float posX = x * cellSize;      //x 좌표를 셀 크기만큼 곱하여 실제 위치 계산
        float posY = y * cellSize;      //y 좌표를 셀 크기만큼 곱하여 실제 위치 계산
        return new Vector3(posX, posY, 0) + transform.position;     //보드 매니저의 위치를 기준으로 실제 좌표 계산
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



    // 업데이트 함수
    void Update()
    {
        
    }
}
