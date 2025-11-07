using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CallMonsterTrap : Trap
{
    private List<EnemyBT> _monsters = new();
    private EnemyBT _choosedMonster;

    #region Initialize Monsters On Map
    private void OnEnable()
    {
        EventBus.Subscribe<EnemyBT>(EventType.SpawnEnemy, AddMonsters);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyBT>(EventType.SpawnEnemy, AddMonsters);
    }
    private void AddMonsters(EnemyBT enemy)
    {
        if (!_monsters.Contains(enemy))
        {
            _monsters.Add(enemy);
        }
    }
    #endregion

    protected override void OnTrapActivatedServer(Player player)
    {
        _choosedMonster = _monsters.OrderBy(m => Vector3.Distance(m.transform.position, transform.position)).First();
        if (_choosedMonster)
        {
            BlockPlayer(player, true);
            _choosedMonster.ForceChasePlayer(player.transform);
        }
    }
    protected override void OnTrapEffectFinishedServer(Player player)
    {
        base.OnTrapEffectFinishedServer(player);

        if (_choosedMonster == null) return;
        BlockPlayer(player, false);
    }

    private void BlockPlayer(Player player, bool value)
    {
        player.GetComponent<PlayerController>().BlockPlayerInput(value);
    }
}