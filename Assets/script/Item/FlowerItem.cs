using Unity.Netcode;
public class FlowerItem : Item
{
    public override void Interact(Player player)
    {
        base.Interact(player);

        EventBus.Publish<Item>(EventType.PickFlower, this); //Publish event type PickFlower with FlowerItem in param

        print($"Add {_itemName} +1 to global objectiv, send to monster your localisazion.. Good luck");
        DestroyItemServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyItemServerRpc()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Tous les clients verront l’objet disparaître
            NetworkObject.Despawn(true);
        }
    }

}