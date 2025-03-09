using Fusion.Addons.KCC;
using UnityEngine;

public class PadProcessor : KCCProcessor
{
    public float ImpulseStrength;
    [SerializeField] private bool _isSpherical;
    [SerializeField] private AudioSource _padSFX;

    public override void OnEnter(KCC kcc, KCCData data)
    {
        Vector3 impulseDirection;
        if (_isSpherical)
        {
            // If spherical, the impulse should be a normalized vector towards the closest contacting point of the collider from the center of the pad
            impulseDirection = Vector3.Normalize(kcc.Collider.ClosestPoint(transform.position) - transform.position);
        }
        else
        {
            impulseDirection = transform.forward;
        }
        kcc.SetDynamicVelocity(data.DynamicVelocity - Vector3.Scale(data.DynamicVelocity, impulseDirection.normalized));
        kcc.AddExternalImpulse(impulseDirection * ImpulseStrength);
        _padSFX.Play();
    }
}