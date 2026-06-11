# 대마법사 수행길

# 기능 구현 설명 및 정리



리지드바디나, 박스 컬리더를 넣지 않고 int\[,] grid = new int\[7, 12] 형식의 2차원 배열을 이용해 뿌요판을 구현.



충돌 판정은 IsEmpty, IsValidPosition의 인덱스 값을 이용해 계산.



**링크:https://drive.google.com/drive/folders/1Ks\_3LZ\_VwPHDX5DGFWfnMVS6Is7AVvHZ**



**같은 색깔이 4개 이상 모였는지 찾을 때 사용한 방식**



* 검사를 시작할 뿌요의 좌표를 Queue(대기열)에 넣음.
* 대기열에서 하나를 꺼내 상, 하, 좌, 우 4방향을 검사.
* 조건(보드 안쪽인가? 색이 같은가? 처음 방문했나?)에 맞으면 그 좌표를 다시 대기열에 넣음. (visited 배열을 써서 무한 루프 방지)
* 대기열이 텅 빌 때까지 반복한 후, 찾은 개수(List.Count)가 4 이상인지 확인.



**코루틴(Coroutine)을 이용한 비동기 연쇄 처리**

IEnumerator와 yield return new WaitForSeconds(0.5f)를 사용

코루틴을 써서 유니티의 메인 스레드(화면 그리기 등)는 계속 돌아가게 내버려 둔 채, 해당 함수의 실행만 0.5초 동안 뒤로 미루는걸로 화면이 멈추지 않고 자연스레 콤보 연출



**공중에 뜬 블럭이 떨어지는 알고리즘**

y = 1부터 y = height까지 아래에서 위로(Bottom-Up) 스캔

블록을 발견하면 while문을 써서 그 블록의 아래쪽(targetY - 1)이 빈칸(0)인지 계속 파고 내려가고, 바닥을 찾으면 배열의 데이터를 그곳으로 복사(grid\[x, targetY] = grid\[x, y])하고 기존 자리는 0으로 지우는 메모리 스와핑 방식.



**뿌요 분리 방식**

바닥에 닿아서 LockPuyos()가 실행될 때, bool mainIsLower = mainPuyoPos.y <= subPos.y; 로 Main과 Sub 중 누가 더 아래(Y값이 작은지)에 있는지 수학적으로 계산

무조건 아래에 있는 녀석부터 DropAndLock 함수로 바닥까지 떨어뜨려 배열에 굳힘. 그 다음 위에 있는 녀석을 떨어뜨린다.

