using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 필수

public class PersonaUIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button gameStartButton; // 게임 시작 버튼
    [SerializeField] private Button noButton;         // 아니오(취소) 버튼

    [Header("UI Objects")]
    [SerializeField] private RectTransform mainMenuPanel;     // 메인 메뉴 전체 패널 (지진 흔들림 연출용)
    [SerializeField] private RectTransform leftButtonsParent; // 왼쪽 버튼 그룹 (퇴장/복구 대상)
    [SerializeField] private RectTransform characterIllust;  // 캐릭터 일러스트 (1920x1080 제자리 회전)
    [SerializeField] private Image characterImage;            // 캐릭터 Image 컴포넌트
    [SerializeField] private RectTransform confirmMenuPanel;  // 확인창 패널

    [Header("Character Sprites")]
    [SerializeField] private Sprite normalPose;  // 최초 기본 포즈 일러스트 (1920x1080)
    [SerializeField] private Sprite actionPose;  // 회전 도중 교체될 포즈 일러스트 (1920x1080)

    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.5f; // 회전 시간 (더 역동적이도록 0.5초로 세팅)
    private Vector3 target3DRotation = new Vector3(0f, -360f, 360f);

    [Header("Slam Effect Settings")]
    [SerializeField] private float shakeDuration = 0.25f; // 지진 흔들림 시간
    [SerializeField] private float shakeStrength = 35f;   // 지진 흔들림 강도

    private Vector2 originalButtonsPos;
    private bool isAnimating = false;
    private Tween swapDelayTween; // 이미지 선교체 타이밍용 트윈

    private void Start()
    {
        if (confirmMenuPanel != null) confirmMenuPanel.gameObject.SetActive(false);
        if (characterImage != null) characterImage.sprite = normalPose;

        if (characterIllust != null)
        {
            characterIllust.localRotation = Quaternion.identity;
        }

        if (leftButtonsParent != null)
        {
            originalButtonsPos = leftButtonsParent.anchoredPosition;
        }

        if (gameStartButton != null) gameStartButton.onClick.AddListener(OnClickGameStart);
        if (noButton != null) noButton.onClick.AddListener(OnClickNo);
    }

    // [게임 시작] 버튼 클릭 시
    private void OnClickGameStart()
    {
        if (isAnimating) return;
        isAnimating = true;

        KillActiveTweens();

        // 1. 기존 왼쪽 버튼들 빠르게 퇴장
        leftButtonsParent.DOAnchorPos(originalButtonsPos + new Vector2(-600f, 0f), transitionDuration * 0.4f)
            .SetEase(Ease.InQuad);

        // 2. 회전이 다 끝나기 전(전체 회전 시간의 75% 지점)에 이미지를 미리 교체합니다.
        float spriteSwapDelay = transitionDuration * 0.75f;
        swapDelayTween = DOVirtual.DelayedCall(spriteSwapDelay, () =>
        {
            if (characterImage != null && actionPose != null)
            {
                characterImage.sprite = actionPose;
            }
        });

        // 3. 캐릭터 일러스트 회전 실행
        // Ease.InCubic을 사용해 점점 회전이 빨라지다가, 한계 속도에서 갑자기 정지하도록 만듭니다.
        characterIllust.DORotate(target3DRotation, transitionDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                OnRotationComplete();
            });
    }

    // 회전이 "쾅" 멈춘 직후 시점
    private void OnRotationComplete()
    {
        // 1. 거친 지진 흔들림 적용 (부드러운 위아래 반동 완전 삭제)
        // 강도(shakeStrength)를 높이고, 진동수(vibrato)를 40으로 크게 올려 아주 빠르고 억센 충격을 줍니다.
        // 다섯 번째 인자인 snapping 값을 true로 설정하여 부드러운 소수점 보간 없이 딱딱 끊어지며 흔들리게 처리했습니다.
        mainMenuPanel.DOShakePosition(shakeDuration, shakeStrength, 40, 90f, true, true)
            .OnComplete(() => {
                isAnimating = false;
            });

        // 2. 확인창 등장 (0.08초 만에 팍!)
        if (confirmMenuPanel != null)
        {
            confirmMenuPanel.gameObject.SetActive(true);
            confirmMenuPanel.localScale = Vector3.zero;
            confirmMenuPanel.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutQuad);
        }
    }

    // [아니오] 버튼 클릭 시 (복구)
    private void OnClickNo()
    {
        if (isAnimating) return;
        isAnimating = true;

        KillActiveTweens();

        // 1. 확인창 빠르게 팍 사라짐 (0.1초)
        confirmMenuPanel.DOScale(Vector3.zero, 0.1f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                confirmMenuPanel.gameObject.SetActive(false);
            });

        // 2. 복구 회전이 시작되자마자 이미지 원래대로 교체
        float reverseSwapDelay = transitionDuration * 0.2f;
        swapDelayTween = DOVirtual.DelayedCall(reverseSwapDelay, () =>
        {
            if (characterImage != null && normalPose != null)
            {
                characterImage.sprite = normalPose;
            }
        });

        // 3. 원래 각도로 원상복구 회전
        characterIllust.DORotate(Vector3.zero, transitionDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                isAnimating = false;
            });

        // 4. 왼쪽 기존 버튼들 원위치 복원
        leftButtonsParent.DOAnchorPos(originalButtonsPos, transitionDuration)
            .SetEase(Ease.OutCubic);
    }

    private void KillActiveTweens()
    {
        characterIllust.DOKill();
        leftButtonsParent.DOKill();
        confirmMenuPanel.DOKill();
        mainMenuPanel.DOKill();

        if (swapDelayTween != null && swapDelayTween.IsActive())
        {
            swapDelayTween.Kill();
        }
    }
}