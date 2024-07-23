using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


/// <summary>
/// Basic launch command processor (Multiplay prefers passing IP and port along)
/// </summary>
public class ApplicationData
{
    /// <summary>
    /// Commands Dictionary
    /// Supports flags and single variable args (eg. '-argument', '-variableArg variable')
    /// </summary>
    Dictionary<string, Action<string>> m_CommandDictionary = new Dictionary<string, Action<string>>();

    // TODO: Disable on final production build
    const string k_IsLocDevCmd = "isLocDev";
    // -------------------------------------------
    const string k_IPCmd = "ip";
    const string k_PortCmd = "port";
    const string k_SteamLobbyCmd = "connect_lobby";
    // WASD
    const string k_PositionIDCmd = "pos_id";
    // const string k_QueryPortCmd = "queryPort";

    //Disables some behaviour we don't want to test with the serverTests
    // public static bool IsServerUnitTest;

    public bool IsLocalDevelopment = false;
    public string IP = EnvironmentSettings.Settings.LocalGameConfig.IP;
    public ushort Port = EnvironmentSettings.Settings.LocalGameConfig.Port;
    public ulong SteamLobby;
    public int PosID = -1;


  /*  public static string IP()
    {
        return PlayerPrefs.GetString(k_IPCmd);
    }

    public static int Port()
    {
        return PlayerPrefs.GetInt(k_PortCmd);
    }

    public static int QPort()
    {
        return PlayerPrefs.GetInt(k_QueryPortCmd);
    }
*/
    //Ensure this gets instantiated Early on
    public ApplicationData()
    {
        // SetQueryPort("7787");
        m_CommandDictionary["-" + k_IPCmd] = SetIP;
        m_CommandDictionary["-" + k_PortCmd] = SetPort;
        m_CommandDictionary["-" + k_IsLocDevCmd] = SetIsLocDev;
        m_CommandDictionary["+" + k_SteamLobbyCmd] = SetSteamLobby;
        // WASD
        m_CommandDictionary["-" + k_PositionIDCmd] = SetPosID;
        // m_CommandDictionary["-" + k_QueryPortCmd] = SetQueryPort;
        ProcessCommandLinearguments(Environment.GetCommandLineArgs());
    }

    void ProcessCommandLinearguments(string[] args)
    {

        //string[] args = new string[] {"-ip", "0.0.0.0", "-port", "7780", "-queryport", "7781"}; //, "-logFile", "matchplaylog.log"};


        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Launch Args: ");
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = "";
            if (i + 1 < args.Length) // if we are evaluating the last item in the array, it must be a flag
                nextArg = args[i + 1];
            if (EvaluatedArgs(arg, nextArg))
            {
                sb.Append(arg);
                sb.Append(" : ");
                sb.AppendLine(nextArg);
                i++;
            }
        }

        Debug.Log("sb =" + sb);
    }

    /// <summary>
    /// Commands and values come in the args array in pairs, so we
    /// </summary>
    bool EvaluatedArgs(string arg, string nextArg)
    {
        Debug.Log("EvaluatedArgs " + arg  + " " + IsCommand(arg) + " with " + nextArg);        
        if (!IsCommand(arg))
            return false;
        if (IsCommand(nextArg)) // If you have need for flags, make a separate dict for those.
        {
            return false;
        }

        m_CommandDictionary[arg].Invoke(nextArg);
        return true;
    }

    void SetIP(string ipArgument)
    {
        IP = ipArgument;
    }

    void SetPort(string portArgument)
    {
        Debug.Log("Set port");
        if (ushort.TryParse(portArgument, out ushort parsedPort))
        {
            Debug.Log("Set port k_PortCmd = " + k_PortCmd + " parsedPort = " + parsedPort.ToString());
            Port = parsedPort;            
        }
        else
        {
            Debug.LogError($"{portArgument} does not contain a parseable port!");
        }
    }

    void SetIsLocDev(string isLocDevArgument)
    {
        if (bool.TryParse(isLocDevArgument, out bool parsedIsLocDev))
        {
            IsLocalDevelopment = parsedIsLocDev;            
        }
        else
        {
            Debug.LogError($"{isLocDevArgument} does not contain a parseable boolean!");
        }
    }

    void SetSteamLobby(string steamArgument)
    {
        Debug.Log("Set Steam Lobby");
        if (ulong.TryParse(steamArgument, out ulong parsedSteam))
        {
            Debug.Log("Set steam k_SteamLobbyCmd = " + k_SteamLobbyCmd + " parsedPort = " + parsedSteam.ToString());
            SteamLobby = parsedSteam;            
        }
        else
        {
            Debug.LogError($"{steamArgument} does not contain a parseable port!");
        }
    }

    void SetPosID(string posIDArgument)
    {
        Debug.Log("Set PosID");
        if (int.TryParse(posIDArgument, out int parsedPosID))
        {
            Debug.Log("Set PosID k_PortCmd = " + k_PortCmd + " parsedPosID = " + parsedPosID.ToString());
            PosID = parsedPosID;            
        }
        else
        {
            Debug.LogError($"{posIDArgument} does not contain a parsable PosID!");
        }
    }

    /*void SetQueryPort(string qPortArgument)
    {
        if (int.TryParse(qPortArgument, out int parsedQPort))
        {
            PlayerPrefs.SetInt(k_QueryPortCmd, parsedQPort);
        }
        else
        {
            Debug.LogError($"{qPortArgument} does not contain a parseable query port!");
        }
    }*/

    bool IsCommand(string arg)
    {
        return !string.IsNullOrEmpty(arg) && m_CommandDictionary.ContainsKey(arg) && ( arg.StartsWith("-") || arg.StartsWith("+"));
    }
}
