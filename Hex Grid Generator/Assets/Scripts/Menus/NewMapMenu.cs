using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    [SerializeField] HexGrid _hexGrid;

    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
    
    public void CreateSmallMap() => CreateMap(10, 5);

    public void CreateMediumMap() => CreateMap(20, 10);

    public void CreateLargeMap() => CreateMap(30, 15);

    void CreateMap(int x, int z)
    {
        _hexGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }
}