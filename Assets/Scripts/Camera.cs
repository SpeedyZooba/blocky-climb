using UnityEngine;

public class Camera : MonoBehaviour
{
    public static Camera Instance { get; private set; }
    private Transform _target;

    public void SetTarget(Transform target) 
    {  
        _target = target; 
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Extra instance of Camera found.");
        }
    }

    private void LateUpdate()
    {
        if (_target != null)
        {
            transform.SetPositionAndRotation(_target.position, _target.rotation);
        }
    }
}