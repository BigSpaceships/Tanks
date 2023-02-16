using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayGUIManager : MonoBehaviour {
    [SerializeField] private List<GameObject> hostOptions;
    [SerializeField] private List<GameObject> clientOptions;
    [SerializeField] private List<GameObject> serverOptions;

    private List<GameObject> AllOptions =>
        clientOptions.Union(hostOptions.Union(serverOptions)).ToList();

    public void UpdateOptionDisplays(int option) {
        foreach (var optionObject in AllOptions) {
            optionObject.SetActive(false);
        }
        
        var optionArray = new[] {hostOptions, clientOptions, serverOptions};

        foreach (var optionsToDisplay in optionArray[option]) {
            optionsToDisplay.SetActive(true);
        }
    }
}
