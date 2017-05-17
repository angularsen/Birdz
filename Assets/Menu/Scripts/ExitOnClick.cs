// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitOnClick : MonoBehaviour
{
    public void Exit()
    {
        Application.Quit();
    }
}