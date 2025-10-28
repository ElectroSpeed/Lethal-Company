using TMPro;
using UnityEngine;

public class MainMenusManager : MonoBehaviour
{
    [Header("General UI Ref")]
    [SerializeField] private GameObject _buttonPanel;
    [SerializeField] private GameObject _gameTitle;
    [SerializeField] private GameObject _MainMenusBackground;

    [Header("Join Room Ref")]
    [SerializeField] private GameObject _joinRoomPanel;
    [SerializeField] private TMP_InputField _chooseSpeudoJoin;
    [SerializeField] private TMP_InputField _joinCode;

    [Header("Host Room Ref")]
    [SerializeField] private GameObject _hostRoomPanel;
    [SerializeField] private TMP_InputField _chooseSpeudoHost;

    [Header("Settings Ref")]
    [SerializeField] private GameObject _settingsPanel;

    #region JoinPanel
    public void JoinRoom()
    {
        _joinRoomPanel.SetActive(true);
        _buttonPanel.SetActive(false);
    }
    public void QuiJoinPanel()
    {
        _joinRoomPanel.SetActive(false);
        _buttonPanel.SetActive(true);
        _chooseSpeudoJoin.text = string.Empty;
        _joinCode.text = string.Empty;
    }
    #endregion

    #region HostPanel
    public void HostRoom()
    {
        _hostRoomPanel.SetActive(true);
        _buttonPanel.SetActive(false);
    }
    public void QuitHostPanel()
    {
        _hostRoomPanel.SetActive(false);
        _buttonPanel.SetActive(true);
        _chooseSpeudoHost.text = string.Empty;
    }
    #endregion

    #region Setting
    public void Setting()
    {
        _buttonPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }
    public void QuitSetting()
    {
        _buttonPanel.SetActive(true);
        _settingsPanel.SetActive(false);
    }
    #endregion

    #region Quit
    public void Quit()
    {
        Application.Quit();
    }
    #endregion
}

