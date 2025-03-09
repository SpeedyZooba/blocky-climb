using Fusion;
using Fusion.Addons.KCC;
using System;
using System.Collections;
using UnityEngine;

// NetworkBehaviour is associated with NetworkObjects, which are the equivalent of GameObjects in Fusion
public class Player : NetworkBehaviour
{
    #region Player Information
    public Base PlayerBase;
    public bool IsReady;
    [Networked] public string Nickname { get; private set; }
    [Networked] public float HealthPoints { get; set; }
    public double Score => Math.Round(transform.position.y, 1);
    public static event Action<NetworkObject> OnDeath;
    public static event Action<NetworkObject> OnReady;
    #endregion
    #region Physics
    [field: SerializeField] public float LaserRange {  get; private set; }
    [field: SerializeField] public float HookRange { get; private set; }
    [field: SerializeField] public float BreakRange { get; private set; }
    [Networked] public float RemainingGlide { get; private set; }
    [Networked] public bool IsGliding { get; private set; }
    private Transform _transform;
    private float _glideDrain;
    [SerializeField] private KCC _kcc;
    [SerializeField] private KCCProcessor _glideKcc;
    [SerializeField] private Transform _camTarget;
    [SerializeField] private float _camSensitivity;
    [SerializeField] private float _maxPitch;
    [SerializeField] private Vector3 _jumpImpulse;
    [SerializeField] private float _doubleJumpMultiplier;
    [SerializeField] private float _doubleJumpCooldown;
    [SerializeField] private float _grapplingHookCooldown;
    [SerializeField] private float _grapplingHookForce;
    [SerializeField] private float _glideDuration;
    [SerializeField] private float _glideCooldown;
    [SerializeField] private float _breakCooldown;
    [SerializeField] private float _laserDuration;
    [SerializeField] private float _laserDamage;
    [SerializeField] private float _laserCooldown;

    private InputManager _inputManager;
    private Vector2 _baseLookRotation;
    #endregion
    #region Controls
    public float DoubleJumpRemaining => (_doubleJumpTimer.RemainingTime(Runner) ?? 0f) / _doubleJumpCooldown;
    public float GrapplingHookRemaining => (_grapplingHookTimer.RemainingTime(Runner) ?? 0f) / _grapplingHookCooldown;
    public float GlidingRemaining => (_glideTimer.RemainingTime(Runner) ?? 0f) / _glideCooldown;
    public float BreakRemaining => (_breakTimer.RemainingTime(Runner) ?? 0f) / _breakCooldown;
    public float LaserRemaining => (_laserTimer.RemainingTime(Runner) ?? 0f) / _laserCooldown;
    [SerializeField] private LineRenderer _rightEye;
    [SerializeField] private Transform _laserSpawn;
    [SerializeField] private MeshRenderer[] _playerParts;
    [SerializeField] private AudioSource _jumpSFX;
    [SerializeField] private AudioSource _hookSFX;
    [SerializeField] private AudioSource _laserSFX;
    [SerializeField] private AudioSource _breakSFX;
    [SerializeField] private AudioSource _deathSFX;
    [Networked] private NetworkButtons _previouslyPressed { get; set; }
    [Networked] private TickTimer _doubleJumpTimer { get; set; }
    [Networked] private TickTimer _grapplingHookTimer { get; set; }
    [Networked] private TickTimer _glideTimer { get; set; }
    [Networked] private TickTimer _breakTimer { get; set; }
    [Networked] private TickTimer _laserTimer { get; set; }
    [Networked, OnChangedRender(nameof(PlayJumpSFX))] private int _jumpSync { get; set; }
    [Networked, OnChangedRender(nameof(PlayHookSFX))] private int _hookSync { get; set; }
    [Networked, OnChangedRender(nameof(PlayLaserSFX))] private int _laserSync { get; set; }
    [Networked, OnChangedRender(nameof(PlayBreakSFX))] private int _breakSync { get; set; }
    #endregion

    public override void Spawned()
    {
        _glideDrain = 1f / (_glideDuration * Runner.TickRate);
        RemainingGlide = 1f;
        if (HasInputAuthority)
        {
            name = PlayerPrefs.GetString("Photon.Menu.Username");
            HealthPoints = 50;
            RPC_Nickname(name);
            foreach (MeshRenderer part in _playerParts) 
            {
                part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            _inputManager = Runner.GetComponent<InputManager>();
            _inputManager.LocalPlayer = this;
            Camera.Instance.SetTarget(_camTarget);
            UIManager.Instance.LocalPlayer = this;
            UIManager.Instance.SetAdminPanel();
            _kcc.Settings.ForcePredictedLookRotation = true;
        }
    }

    // Networked equivalent of FixedUpdate()
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetInput input))
        {
            Jump(input);
            Glide(input);
            _kcc.AddLookRotation(input.LookDelta * _camSensitivity, -_maxPitch, _maxPitch);
            UpdateCamera();

            if (input.Buttons.WasPressed(_previouslyPressed, InputButton.Grapple)) 
            {
                ShootGrapplingHook(_camTarget.forward);
            }

            if (IsGliding && !CanGlide())
            {
                ToggleGliding(false);
            }

            if (input.Buttons.WasPressed(_previouslyPressed, InputButton.Laser)) 
            {
                ShootLaser(_camTarget.forward);
            }

            if (input.Buttons.WasPressed(_previouslyPressed, InputButton.Break))
            {
                BreakPlatform(_camTarget.forward);
            }

            SetInputDirection(input);
            _previouslyPressed = input.Buttons;
            _baseLookRotation = _kcc.GetLookRotation();
        }
        IsDead();
    }

    // The distinction between FixedUpdate() and LateUpdate() still applies here
    // To fix the jittering at high framerates, call the Render() which executes after each simulation (which is handling the physics -> FixedUpdate())
    public override void Render()
    {
        if (_kcc.Settings.ForcePredictedLookRotation)
        {
            Vector2 predictedLookRotation = _baseLookRotation + _inputManager.AccumulatedMouseDelta * _camSensitivity;
            _kcc.SetLookRotation(predictedLookRotation);
        }
        UpdateCamera();
    }

    public void ResetCooldowns()
    {
        _doubleJumpTimer = TickTimer.None;
        _grapplingHookTimer = TickTimer.None;
        _glideTimer = TickTimer.None;
        _breakTimer = TickTimer.None;
        _laserTimer = TickTimer.None;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _kcc.SetPosition(position);
        _kcc.SetLookRotation(rotation);
        PlayerBase.SpawnPoint = position;
        PlayerBase.SpawnRotation = rotation;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority | RpcTargets.StateAuthority)]
    public void RPC_SetReady()
    {
        IsReady = true;
        OnReady?.Invoke(Object);
        if (HasInputAuthority)
        {
            if (!HasStateAuthority)
            {
                UIManager.Instance.PlayerIsReady();
            }
        }
    }
    
    private void SetInputDirection(NetInput input)
    {
        Vector3 worldDirection;
        if (IsGliding)
        {
            RemainingGlide = Mathf.Max(0f, RemainingGlide - _glideDrain);
            worldDirection = _kcc.Data.TransformDirection;
        }
        else
        {
            worldDirection = _kcc.FixedData.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y);
        }
        _kcc.SetInputDirection(worldDirection);
    }

    private void Jump(NetInput input)
    {
        if (input.Buttons.WasPressed(_previouslyPressed, InputButton.Jump))
        {
            if (_kcc.FixedData.IsGrounded) 
            {
                _kcc.Jump(_jumpImpulse);
            }
            else if (_doubleJumpTimer.ExpiredOrNotRunning(Runner))
            {
                _kcc.Jump(_jumpImpulse * _doubleJumpMultiplier);
                ToggleGliding(false);
                _doubleJumpTimer = TickTimer.CreateFromSeconds(Runner, _doubleJumpCooldown);
                ++_jumpSync;
            }
        }
    }

    private void ShootGrapplingHook(Vector3 lookDirection)
    {
        if (_grapplingHookTimer.ExpiredOrNotRunning(Runner) && Physics.Raycast(_camTarget.position, lookDirection, out RaycastHit hitInfo, HookRange))
        {
            _grapplingHookTimer = TickTimer.CreateFromSeconds(Runner, _grapplingHookCooldown);
            if (hitInfo.collider.TryGetComponent(out Block _) || hitInfo.collider.TryGetComponent(out Player player))
            {
                Vector3 hookVector = Vector3.Normalize(hitInfo.point - _transform.position);
                if (hookVector.y > 0f)
                {
                    hookVector = Vector3.Normalize(hookVector + Vector3.up);
                    _kcc.Jump(hookVector * _grapplingHookForce);
                    ++_hookSync;
                    ToggleGliding(false);
                }
            }
        }
    }

    private void BreakPlatform(Vector3 lookDirection)
    {
        if (_breakTimer.ExpiredOrNotRunning(Runner) && Physics.Raycast(_camTarget.position, lookDirection, out RaycastHit hitInfo, BreakRange))
        {
            if (hitInfo.collider.TryGetComponent(out Block block))
            {
                if (!block.CompareTag("Finish"))
                {
                    RPC_MakeBreakRequest(block);
                }
            }
        }
    }

    private void ShootLaser(Vector3 lookDirection)
    {
        _rightEye.SetPosition(0, _laserSpawn.position);
        if (_laserTimer.ExpiredOrNotRunning(Runner) && Physics.Raycast(_camTarget.position, lookDirection, out RaycastHit hitInfo, LaserRange))
        {
            if (hitInfo.collider.TryGetComponent(out Player _))
            {
                StartCoroutine(PlayShootFX());
                _laserTimer = TickTimer.CreateFromSeconds(Runner, _laserCooldown);
                Vector3 targetStruck = hitInfo.point;
                _rightEye.SetPosition(1, targetStruck);
                ++_laserSync;
                Player targetPlayer = hitInfo.collider.GetComponent<Player>();
                InflictLaserDamage(targetPlayer);
            }
        }
        else if (_laserTimer.ExpiredOrNotRunning(Runner))
        {
            StartCoroutine(PlayShootFX());
            _laserTimer = TickTimer.CreateFromSeconds(Runner, _laserCooldown);
            _rightEye.SetPosition(1, _camTarget.position + lookDirection * LaserRange);
            ++_laserSync;
        }
    }

    private void Glide(NetInput input)
    {
        if (input.Buttons.WasPressed(_previouslyPressed, InputButton.Glide) && _glideTimer.ExpiredOrNotRunning(Runner) && CanGlide())
        {
            ToggleGliding(true);
        }
        else if (input.Buttons.WasReleased(_previouslyPressed, InputButton.Glide) && IsGliding)
        {
            ToggleGliding(false);
        }
    }

    private bool CanGlide()
    {
        return !_kcc.Data.IsGrounded && RemainingGlide > 0f;
    }

    private void ToggleGliding(bool isGliding)
    {
        if (IsGliding == isGliding)
        {
            return;
        }

        if (isGliding)
        {
            _kcc.AddModifier(_glideKcc);
            Vector3 glidingVelocity = _kcc.Data.DynamicVelocity;
            glidingVelocity.y *= 0.10f;
            _kcc.SetDynamicVelocity(glidingVelocity);
        }
        else
        {
            _kcc.RemoveModifier(_glideKcc);
            RemainingGlide = 1f;
            _glideTimer = TickTimer.CreateFromSeconds(Runner, _glideCooldown);
        }
        IsGliding = isGliding;
    }

    private void IsDead()
    {
        if (HealthPoints <= 0)
        {
            OnDeath?.Invoke(Object);
            PlayDeathSFX();
        }
    }

    private void Awake()
    {
        _transform = transform;
    }

    private void UpdateCamera()
    {
        _camTarget.localRotation = Quaternion.Euler(_kcc.GetLookRotation().x, 0f, 0f);
    }

    private void InflictLaserDamage(Player player)
    {
        player.HealthPoints -= _laserDamage;
    }

    private void PlayJumpSFX()
    {
        _jumpSFX.Play();
    }

    private void PlayHookSFX()
    {
        _hookSFX.Play();
    }

    private void PlayLaserSFX()
    {
        _laserSFX.Play();
    }

    private void PlayBreakSFX()
    {
        _breakSFX.Play();
    }

    private void PlayDeathSFX()
    {
        _deathSFX.Play();
    }

    private IEnumerator PlayShootFX()
    {
        _rightEye.enabled = true;
        yield return new WaitForSeconds(_laserDuration);
        _rightEye.enabled = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Nickname(string name)
    {
        Nickname = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_MakeBreakRequest(Block block)
    {
        _breakTimer = TickTimer.CreateFromSeconds(Runner, _breakCooldown);
        block.RPC_Break();
        ++_breakSync;
    }
}