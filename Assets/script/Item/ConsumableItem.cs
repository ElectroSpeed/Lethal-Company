using UnityEngine;

public class ConsumableItem : Item, IUseItem
{

    [Header("owner")]
    private Player _player;

    [Header("Feedback")]
    [SerializeField] private AudioSource _useEffectSfx;
    [SerializeField] private GameObject _useEffectVfx;


    [Header("Apply Effect Data")]
    private float validHeader = -999;


    public override void Interact(Player player)
    {
        if (_player == null) _player = player; 

        base.Interact(player);
        if (player._equipiedItem != null) player.Drop();
        player._equipiedItem = this;
    }


    public void Use()
    {
        ApplyEffect();
        Debug.Log($"{_player.name} Use {_itemName}");
        _player.Drop(); // need to call at the end 
    }

    public void ApplyEffect()
    {
        ApplySfx();
        ApplyVfx();



    }
    private void ApplySfx()
    {

    }
    private void ApplyVfx()
    {

    }
}