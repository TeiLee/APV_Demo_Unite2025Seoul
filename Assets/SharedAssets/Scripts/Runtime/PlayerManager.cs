using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Benchmarking;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// This class will enable the touch input canvas on handheld devices and will trigger the camera flythrough if the player is idle
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private bool m_FlythroughWhenIdle;
    [SerializeField] private float m_IdleTransitionTime;
    [SerializeField] private GameObject m_CrosshairCanvas;
    [SerializeField] private GameObject m_TouchInputCanvas;
    [SerializeField] private GameObject m_EventSystem;
    
    public PlayableDirector FlythroughDirector;
    
    private bool m_InFlythrough;
    private float m_TimeIdle;
    private CinemachineCamera m_VirtualCamera;
    private bool m_HasFocus;
    
    private PlayerInput inputActions;
    
    
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private float middleTimePosition = 8f; // 중간 시간 위치 (초 단위)
    private bool isPlaying = false;
    
    
    void Start()
    {
        if (EventSystem.current == null)
        {
            m_EventSystem.SetActive(true);
        }
        
        if (PerformanceTest.RunningBenchmark)
        {
            Destroy(gameObject);
            return;
        }
        
        m_InFlythrough = false;

        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            m_TouchInputCanvas.SetActive(true);
        }

        NotifyPlayerMoved();

        m_VirtualCamera = GetComponentInChildren<CinemachineCamera>();
        EnableFlythrough();
    }

    void Update()
    {
        if (m_FlythroughWhenIdle && m_TimeIdle > m_IdleTransitionTime && !m_InFlythrough)
        {
            m_TimeIdle = 0;
            EnableFlythrough();
        }


        #if UNITY_EDITOR
        if(m_HasFocus) m_TimeIdle += Time.unscaledDeltaTime;
        #else 
        m_TimeIdle += Time.unscaledDeltaTime;
        #endif
        
        // 마우스 입력 처리
        if (Input.GetMouseButtonDown(1)) // 왼쪽 클릭
        {
            ToggleTimeline();
        }
        else if (Input.GetMouseButtonDown(0)) // 오른쪽 클릭
        {
            ChangeTimelinePosition();
        }
        
    }

    
    private void ToggleTimeline()
    {
        if (isPlaying)
        {
            timelineDirector.Pause();
            isPlaying = false;
        }
        else
        {
            timelineDirector.Play();
            isPlaying = true;
        }
    }

    private void ChangeTimelinePosition()
    {
        if (timelineDirector.time == 0)
        {
            timelineDirector.time = middleTimePosition;
        }
        else
        {
            timelineDirector.time = 0;
        }
    }
    
    
    private void Awake()
    {
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public void EnableFlythrough()
    {

        if (FlythroughDirector == null)
        {
            m_InFlythrough = true;
        }
        else
        {
            FlythroughDirector.gameObject.SetActive(true);
        
            TimelineAsset timeline = FlythroughDirector.playableAsset as TimelineAsset;
            FlythroughDirector.SetGenericBinding(timeline.GetOutputTrack(0), CinemachineCore.FindPotentialTargetBrain(m_VirtualCamera));
        
            FlythroughDirector.time = 0;
            FlythroughDirector.Play();
            m_InFlythrough = true;
            m_CrosshairCanvas.SetActive(false);

            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                m_TouchInputCanvas.SetActive(false);
            }
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        m_HasFocus = hasFocus;
    }

    public void EnableFirstPersonController()
    {

        m_CrosshairCanvas.SetActive(true);
        
        if (FlythroughDirector != null)
        {
            FlythroughDirector.gameObject.SetActive(false);
        }
        m_InFlythrough = false;
        
    }

    public void NotifyPlayerMoved()
    {
        m_TimeIdle = 0;
        if (m_InFlythrough)
        {
            EnableFirstPersonController();
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                m_TouchInputCanvas.SetActive(true);
            }
        }
    }
}
