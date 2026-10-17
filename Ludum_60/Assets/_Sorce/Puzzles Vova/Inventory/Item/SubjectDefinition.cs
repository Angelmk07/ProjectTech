using UnityEngine;

public class SubjectDefinition : MonoBehaviour
{
    public ItemsState state;
    public string GetItemState()
    {
        return state.ToString();
    }
}
