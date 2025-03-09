using Fusion;
using UnityEngine;

public class Block : NetworkBehaviour
{
    private Renderer _blockRenderer;
    [SerializeField] private Material _highlightingMaterial;
    [SerializeField] private Material _defaultMaterial;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Break()
    {
        if (!gameObject.CompareTag("Finish"))
        {
            _blockRenderer.material = _defaultMaterial;
            gameObject.SetActive(false);
        }
    }

    private void OnMouseEnter()
    {
        if (!gameObject.CompareTag("Finish"))
        {
            _blockRenderer.material = _highlightingMaterial;
        }
    }

    private void OnMouseExit()
    {
        if (!gameObject.CompareTag("Finish"))
        {
            _blockRenderer.material = _defaultMaterial;
        }
    }

    private void Awake()
    {
        if (GetComponent<Renderer>() != null)
        {
            _blockRenderer = GetComponent<Renderer>();
        }
        else
        {
            Debug.Log($"Missing renderer found for {gameObject.name}.");
        }
    }
}