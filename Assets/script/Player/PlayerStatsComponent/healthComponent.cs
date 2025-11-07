[System.Serializable]
public class HealthComponent
{
    public bool _isAlive { get; private set; } = true;
    public void ChangePlayerLife(bool newValue) => _isAlive = newValue;
}