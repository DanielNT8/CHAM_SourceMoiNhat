using UnityEngine;

public class ClaimTree : MonoBehaviour
{
    private bool isClaim;

    public bool GetisClaim()
    {
        return isClaim;
    }

    public void SetisClaim(bool value)
    {
        isClaim = value;
    }

    public ClaimTree()
    {
        this.SetisClaim(true);
    }
}
