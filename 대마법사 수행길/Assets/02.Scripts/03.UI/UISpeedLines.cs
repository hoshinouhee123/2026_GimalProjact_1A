using UnityEngine;
using UnityEngine.UI; // UI를 다루기 위해 필수!

public class UISpeedLines : MonoBehaviour
{
    [Header("속도선 설정")]
    public int lineCount = 30;         // 화면에 띄울 선의 개수
    public float speed = 3500f;        // 날아가는 속도 (엄청 빨라야 속도감납니다)
    public float panelHeight = 300f;   // 컷인 패널의 최대 높이 (아까 300으로 설정했었죠!)

    private RectTransform[] lines;

    void Awake()
    {
        // 선들을 담을 배열 준비
        lines = new RectTransform[lineCount];

        for (int i = 0; i < lineCount; i++)
        {
            // 1. 하얀 선 역할을 할 빈 UI 오브젝트 스스로 생성
            GameObject go = new GameObject("SpeedLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(this.transform, false);

            // 2. 색상은 흰색, 투명도는 20%~60% 사이로 랜덤하게 주어 입체감 생성
            Image img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, Random.Range(0.2f, 0.6f));
            img.raycastTarget = false; // 클릭을 방해하지 않게 설정

            RectTransform rt = go.GetComponent<RectTransform>();

            // 3. 두께는 2~8픽셀, 길이는 300~1500픽셀 사이로 랜덤하게 길쭉하게 찢음!
            rt.sizeDelta = new Vector2(Random.Range(300, 1500), Random.Range(2, 8));

            // 4. 패널 안에서 랜덤한 Y 위치(높이) 잡기
            float startX = Random.Range(-1500, 1500);
            float startY = Random.Range(-panelHeight / 2, panelHeight / 2);
            rt.anchoredPosition = new Vector2(startX, startY);

            lines[i] = rt;
        }
    }

    void Update()
    {
        // 매 프레임마다 모든 선들을 왼쪽으로 미친 듯이 이동시킵니다!
        foreach (var rt in lines)
        {
            rt.anchoredPosition += Vector2.left * speed * Time.deltaTime;

            // 선이 화면 왼쪽 밖으로 완전히 뚫고 나가면?
            if (rt.anchoredPosition.x < -2000)
            {
                // 오른쪽 끝으로 다시 소환해서 무한 재활용 (컨베이어 벨트 방식)
                float newY = Random.Range(-panelHeight / 2, panelHeight / 2);
                rt.anchoredPosition = new Vector2(2000, newY);

                // 크기도 다시 랜덤하게 바꿔줌
                rt.sizeDelta = new Vector2(Random.Range(300, 1500), Random.Range(2, 8));
            }
        }
    }
}