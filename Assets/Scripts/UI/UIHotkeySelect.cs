using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHotkeySelect : MonoBehaviour {

    void Update() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                NavigateToPrevious();
            else
                NavigateToNext();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
            NavigateToNext();
        }
    }

    private void NavigateToNext()
    {
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null)
        {
            Selectable currentSelection = selectedObject.GetComponent<Selectable>();
            ApplyEnterSelect(currentSelection.navigation.selectOnRight);
        }
    }

    private void NavigateToPrevious()
    {
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null)
        {
            Selectable currentSelection = selectedObject.GetComponent<Selectable>();
            ApplyEnterSelect(currentSelection.navigation.selectOnLeft);
        }
    }

    private void ApplyEnterSelect(Selectable _selectionToApply) {
        if (_selectionToApply != null) {
            if (_selectionToApply.GetComponent<InputField>() != null) {
                _selectionToApply.Select();
            }
            else {
                Button selectedButton = _selectionToApply.GetComponent<Button>();
                if (selectedButton != null) {
                    _selectionToApply.Select();
                    selectedButton.OnPointerClick(new PointerEventData(EventSystem.current));
                }
            }
        }
    }
}
