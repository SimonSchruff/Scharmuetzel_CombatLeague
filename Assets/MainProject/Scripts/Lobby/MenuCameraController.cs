
using Cinemachine;
using UnityEngine;
using DG.Tweening;

public class MenuCameraController : MonoBehaviour
{
    public static MenuCameraController Instance;

    [SerializeField] private CinemachineVirtualCamera _startCam;
    [SerializeField] private CinemachineVirtualCamera _lobbyCam;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(this); }
        
    }

    private void Start()
    {
        SwitchToStartCam();
    }

    public void RotateCameraYToDegree(float angleY)
    {
        var currentRotation = transform.rotation.eulerAngles;
        transform.DORotate(new Vector3(currentRotation.x, angleY, currentRotation.z), 2.5f, RotateMode.Fast);
    }

    public void SwitchToStartCam()
    {
        if(_startCam == null || _lobbyCam == null) return;
        
        _startCam.gameObject.SetActive(true);
        _lobbyCam.gameObject.SetActive(false);
    }
    
    public void SwitchToLobbyCam()
    {
        if(_startCam == null || _lobbyCam == null) return;

        _startCam.gameObject.SetActive(false);
        _lobbyCam.gameObject.SetActive(true);
    }
}
