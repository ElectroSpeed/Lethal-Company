using UnityEngine;
public class PlayerBaseStatsComponent 
{
    [Header("HealthComponent")]
    [HideInInspector] public HealthComponent _healthComponent = null;

    [Header("SpeedComponent")]
    [HideInInspector] public SpeedComponent _speedComponent = null;

    [Header("StreesComponent")]
    [HideInInspector] public StressComponent _stressComponent = null;

    private void LateUpdate()
    {

    }

    public PlayerBaseStatsComponent(HealthComponent healthComponent, SpeedComponent speedComponent, StressComponent stressComponent)
    {
        _healthComponent = healthComponent;
        _speedComponent = speedComponent;
        _stressComponent = stressComponent;
    }

}