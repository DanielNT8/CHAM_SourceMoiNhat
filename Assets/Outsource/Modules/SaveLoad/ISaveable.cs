using UnityEngine;

public interface ISaveable
{
    string SavedDatas { get; }
    void LoadDatas(string savedDatas);
}
