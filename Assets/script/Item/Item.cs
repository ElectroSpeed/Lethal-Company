using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class Item : MonoBehaviour, IInteractible
{
    [Header("Item Data")]
    [TextArea] public string _itemName;
    public ItemType _itemType;

    [Header("Detection Area")]
    [SerializeField, Min(0.1f)] private float _detectionArea = 2f;

    private SphereCollider _collider;
    private readonly HashSet<Player> _playersInZone = new();

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        _collider = GetComponent<SphereCollider>();
        if( _collider == null ) _collider = gameObject.AddComponent<SphereCollider>();

        _collider.isTrigger = true;
        _collider.radius = _detectionArea;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player))
            _playersInZone.Add(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player))
            _playersInZone.Remove(player);
    }

    public virtual void Interact(Player player)
    {
        if (player == null || !_playersInZone.Contains(player))
            return;

        Debug.Log($"{player.name} interact with {_itemName}");
    }
}
