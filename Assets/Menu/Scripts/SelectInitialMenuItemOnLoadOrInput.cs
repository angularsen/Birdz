// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Ensures an initial menu item is selected (highlighted) on menu load
///     or on input if there are no selected items (if mouse clicked outside menu).
/// </summary>
public class SelectInitialMenuItemOnLoadOrInput : MonoBehaviour
{
    public EventSystem EventSystem;
    public GameObject InitialSelectedItem;

    void Start()
    {
    }

    void OnEnable()
    {
        SelectInitialOrFirstItem();
    }

    void Update()
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (Input.GetAxis(InputNames.Vertical) != 0 && EventSystem.current.currentSelectedGameObject == null)
            SelectInitialOrFirstItem();
    }

    private void SelectInitialOrFirstItem()
    {
        GameObject objectToSelect = InitialSelectedItem != null
            ? InitialSelectedItem
            : transform.GetChild(0).gameObject;
        StartCoroutine(SelectCoroutine(objectToSelect));
    }

    private IEnumerator SelectCoroutine(GameObject objectToSelect)
    {
        // Workaround for highlighting item not working on menu load
        EventSystem.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();

        EventSystem.SetSelectedGameObject(objectToSelect);
        print("SelectOnStart: Selected " + objectToSelect);
    }
}