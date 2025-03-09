using UnityEngine;
using Fusion;
using System;

public class TimeItem : NetworkBehaviour
{
    public static event Action<int, float> OnTaken;
    [SerializeField] private int _changeInSeconds;
    [SerializeField] private float _chanceToDecrease;
    [SerializeField] private float _rotationSpeed;
    private Transform _self;

    private void Start()
    {
        _self = transform;
    }

    private void Update()
    {
        _self.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTaken?.Invoke(_changeInSeconds, _chanceToDecrease);
        Runner.Despawn(Object);
    }
}
