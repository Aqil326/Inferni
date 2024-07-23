using UnityEngine;


[CreateAssetMenu(menuName = "Nakama/Connection data")]
public class ConnectionData : ScriptableObject
{
    #region FIELDS

    [SerializeField] private string scheme = "http";
    [SerializeField] private string hostNakama = "localhost";

    [SerializeField] private int portNakama = 7350;
    [SerializeField] private string serverKey = "defaultkey";


    [SerializeField] private string loginScheme;
    [SerializeField] private string host = "localhost";

    [SerializeField] private int loginPort;
    [SerializeField] private string loginUrl = "";
    [SerializeField] private string equippedWearUrl = "";
    [SerializeField] private string batchUrlPrefix = "";
    [SerializeField] private string platformHost = "localhost";

    #endregion

    #region PROPERTIES

    public string Scheme { get => scheme; }
    public string HostNakama { get => hostNakama; }
    public int PortNakama { get => portNakama; }
    public string ServerKey { get => serverKey; }

    public string LoginUrl { get { return (loginScheme + "://" + host + ":" + loginPort + loginUrl); } }
    public string EquippedWearUrl { get { return (loginScheme + "://" + host + ":" + loginPort + equippedWearUrl); } }
    public string BatchEquippedWearUrl { get { return (loginScheme + "://" + host + ":" + loginPort + equippedWearUrl + batchUrlPrefix); } }
    public string platformURL { get { return (loginScheme + "://" + platformHost + "/?gt_pin="); } }

    #endregion
}