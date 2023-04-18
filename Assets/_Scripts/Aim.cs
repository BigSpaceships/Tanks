using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;

    void Update() {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out var hit)) {
            var tank = GameManager.GetFocusedTank();

            if (tank == null) {
                return;
            }
            
            tank.GetComponent<TankData>().UpdateTargetPosition(hit.point);
        }
    }
}