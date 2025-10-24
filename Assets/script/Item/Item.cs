using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(NetworkObject))]
public class Item : NetworkBehaviour, IInteractible
{
    [Header("Item Data")]
    public string _itemName;
    public ItemType _itemType;

    [Header("Detection Area")]
    [SerializeField, Range(min: 1, max: 10)] private float _detectionArea = 2f;
    private SphereCollider _collider;

    protected virtual void Awake()
    {
        Initialize();
    }
    protected virtual void Initialize()
    {
        _collider = GetComponent<SphereCollider>();
        if (_collider == null) _collider = gameObject.AddComponent<SphereCollider>();

        _collider.isTrigger = true;
        _collider.radius = _detectionArea;
    }
    public virtual void Interact(Player player)
    {
        if (player == null)
            return;

        Debug.Log($"{player.name} interact with {_itemName}");
    }
}
