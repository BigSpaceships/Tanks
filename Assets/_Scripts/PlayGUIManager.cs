using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayGUIManager : MonoBehaviour {
    [SerializeField] private List<GameObject> hostOptions;
    [SerializeField] private List<GameObject> clientOptions;
    [SerializeField] private List<GameObject> serverOptions;

    [SerializeField] private TMP_Dropdown modeDropdown;
    [SerializeField] private TMP_InputField nameField;

    public static PlayGUIManager Manager { get; private set; }

    private List<GameObject> AllOptions =>
        clientOptions.Union(hostOptions.Union(serverOptions)).ToList();

    private void Start() {
        Manager = this;

        // NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
    }

    public void UpdateOptionDisplays(int option) {
        foreach (var optionObject in AllOptions) {
            optionObject.SetActive(false);
        }

        var optionArray = new[] { hostOptions, clientOptions, serverOptions };

        foreach (var optionsToDisplay in optionArray[option]) {
            optionsToDisplay.SetActive(true);
        }
    }

    public void Play() {
        switch (modeDropdown.value) {
            case 0:
                // host
                NetworkManager.Singleton.StartHost();
                break;
            case 1:
                // client
                // NetworkManager.Singleton.GetComponent<WebSocketTransport>().ConnectAddress = clientIpField.text;
                NetworkManager.Singleton.StartClient();
                break;
            case 2:
                // server
                NetworkManager.Singleton.StartServer();
                break;
            default:
                Debug.LogError($"Illegal option {modeDropdown.value}");
                break;
        }

        transform.Find("Play Options").gameObject.SetActive(false);
        transform.Find("In Game").gameObject.SetActive(true);
    }

    public string GetName() {
        return nameField.text;
    }

    public void Disconnect() {
        NetworkManager.Singleton.Shutdown();

        OnDisconnect(0);
    }

    public void OnDisconnect(ulong id) {
        transform.Find("Play Options").gameObject.SetActive(true);
        transform.Find("In Game").gameObject.SetActive(false);

        // SceneManager.LoadScene(SceneManager.GetActiveScene()
        //     .buildIndex); // TODO: properly clean up connection so we don't have to do this
    }
}