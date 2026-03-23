using UnityEngine;

public class Monster : MonoBehaviour
{
    [SerializeField] private string monsterId;
    public string MonsterId => monsterId;

    public void OnCaught()
    {
        ARGameManager.Instance.AddCatch(monsterId);
        Destroy(gameObject);
    }
}
