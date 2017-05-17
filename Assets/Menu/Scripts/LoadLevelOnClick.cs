// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using Assets.Scripts;
using UnityEngine;

public class LoadLevelOnClick : MonoBehaviour
{
    public void LoadByName(string levelName)
    {
        LevelManager.Instance.LoadLevelAsync(levelName);
    }
}