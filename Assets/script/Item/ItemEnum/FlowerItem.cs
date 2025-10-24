using UnityEngine;
using Unity;
public class FlowerItem : Item
{
    public override void Interact(Player player)
    {
        base.Interact(player);

        EventBus.Publish<Item>(EventType.PickFlower, this); //Publish event type PickFlower with FlowerItem in param
    }
}