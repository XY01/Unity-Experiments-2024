using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using TwinCAT.Ads;
using Random = UnityEngine.Random;
using PelicanStudios.Mira;

public class AdsTest : MonoBehaviour
{
    #region  MIRA STRUCTS AND ENUMS

    // // Enums
    // public enum OpsMode_e
    // {
    //     Standby = 0,
    //     Manual = 1,
    //     Run = 2
    // }
    //
    // public enum AllState
    // {
    //     Initializing = 0,
    //     Waiting = 1,
    //     Ready = 2,
    //     Busy = 3,
    //     Done = 4,
    //     Error = 5
    // }
    //
    // public enum PetalState
    // {
    //     NotReady = 0,
    //     Standby = 1,
    //     MovingIn = 2,
    //     MovingOut = 3,
    //     InPosn = 4,
    //     Error = 5
    // }
    //
    //
    //
    //
    //
    //
    //
    // // Structs
    // [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    // public unsafe struct MiraState_s
    // {
    //     public OpsMode_e OpsModeE;
    //     public AllState AllState;
    //     // Pointers to dynamically allocated arrays
    //     //public IntPtr Petals; 
    //     [MarshalAs(UnmanagedType.ByValArray, SizeConst = PetalsNum)]
    //     public PetalInfo[] Petals; // Size should be PetalsNum
    //     public fixed bool SysError[NumAlarms];     // Size should be NumAlarms
    //     //public IntPtr PetalError;
    //     [MarshalAs(UnmanagedType.ByValArray, SizeConst = PetalsNum)]
    //     public PetalErr[] PetalError; // Size should be PetalsNum
    // }
    //
    // [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    // public struct MiraConfig
    // {
    //     public uint PetalOuterMaxPos;
    //     public uint PetalOuterMinPos;
    //     public uint PetalInnerMaxPos;
    //     public uint PetalInnerMinPos;
    //     public uint PetalOuterMaxSpeed;
    //     public uint PetalInnerMaxSpeed;
    //     public uint PetalOuterAccel;
    //     public uint PetalOuterDecel;
    //     public uint PetalInnerAccel;
    //     public uint PetalInnerDecel;
    // }
    //
    // [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    // public struct Petal
    // {
    //     public ushort Position;
    //     public ushort Velocity;
    // }
    //
    // [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    // public struct PetalInfo
    // {
    //     public uint Position;
    //     public PetalState PetalState;
    // }
    //
    // [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    // public struct PetalErr
    // {
    //     public bool Stuck;
    //     public bool OverRun;
    //     public bool Crash;
    // }

    #endregion
   
    // Assuming fixed sizes known at compile time
    public const int PetalsNum = 6;    // Replace with actual size
    public const int NumAlarms = 8;     // Replace with actual size
    // Constants
    private uint EffectorsNumModule = 0;
    private uint MaxPetals = 0;
    private int MaxAccel = 0;
    private int MaxDecel = 0;
    private int PosnHysteresis = 0;
    private int P1_HMvelocity = 0;
    private sbyte ProfPosnModeOp = 0;
    private sbyte ProfVelModeOp = 0;
    private sbyte HomingModeOp = 0;
    private float PetalVel_Max = 0;
    private float PetalVel_Min = 0;
    private float PetalVelScaling = 0;
    
   
    
    private TcAdsClient _tcAdsClient;
    private AdsStream _adsReadStream;
    private AdsStream _adsWriteStream;
    public string AmsNetId = "127.0.0.1";
    private int _port = 851; // Default port for TwinCAT 3

    
    // Example PLC Variables
    private string _sendVariableName = "GlobalVarToSend";
    private string _receiveVariableName = "GlobalVarToReceive";
    
    // External Global Variables
    [SerializeField] private MiraState_s MiraState;
    [SerializeField] private MiraConfig MiraConfig;
    [SerializeField] private Petal[] Goals;
    [SerializeField] private OpsMode_e OpsMode;
    [SerializeField] private bool ActiveError;
 
    
    // Start is called before the first frame update
    void Start()
    {
        // create a new TcClient instance     
        _tcAdsClient = new TcAdsClient();
        _adsReadStream = new AdsStream(4);
        _adsWriteStream = new AdsStream(4);

        Goals = new Petal[PetalsNum];
        MiraConfig = new MiraConfig()
        {
            PetalOuterMaxPos = 0,
            PetalOuterMinPos = 0,
            PetalInnerMaxPos = 0,
            PetalInnerMinPos = 0,
            PetalOuterMaxSpeed = 0,
            PetalInnerMaxSpeed = 0,
            PetalOuterAccel = 0,
            PetalOuterDecel = 0,
            PetalInnerAccel = 0,
            PetalInnerDecel = 0
        };

        MiraState = new MiraState_s();
        MiraState.Petals = new PetalInfo[PetalsNum];
        MiraState.PetalError = new PetalErr[PetalsNum];
        MiraState.SysError = new bool[NumAlarms];
        
        Connect();

        StartCoroutine(ExampleSendAndReceive());
    }

    void Connect()
    {
        AmsAddress serverAddress = null;
        try
        {
            serverAddress = new AmsAddress(AmsNetId, _port);
        }
        catch
        {
            Debug.Log("Invalid AMS NetId or Ams port");
            return;
        }
        
        try
        {
            _tcAdsClient.Connect(serverAddress.NetId, serverAddress.Port);
            Debug.Log("Client port " + _tcAdsClient.Address.Port + " opened. " + _tcAdsClient.ConnectionState);
        
        }
        catch
        {
            Debug.Log("Could not connect client");
        }
    }
    
    IEnumerator ExampleSendAndReceive()
    {
        while (true)
        {
            try
            {
                Debug.Log("Starting ExampleSendAndReceive...");
                // _tcAdsClient.WriteAny(0x4020, 0, valueToWrite);
                // valueToRead = (uint)_tcAdsClient.ReadAny(0x4020, 0, typeof(UInt32));
            
                //ListSymbols(_tcAdsClient);
                
                // MiraStateS = new MiraState_s();
                // MiraStateS.Petals = new PetalInfo[PetalsNum];
                // MiraStateS.PetalError = new PetalErr[PetalsNum];
                //
                // // works
                // int varActiveErrorHandle = _tcAdsClient.CreateVariableHandle("ExtGV.activeError");
                // _tcAdsClient.WriteAny(varActiveErrorHandle, true);
                // Debug.Log("Write activeError success");
                // // works
                // int varSendHandle = _tcAdsClient.CreateVariableHandle("ExtGV.opsMode");
                // _tcAdsClient.WriteAny(varSendHandle, (short)1);
                // Debug.Log("Write opsMode success");
                //
                //
                // int arrayBaseHandle = _tcAdsClient.CreateVariableHandle("ExtGV.goals[2]");
                // int elementIndex = 2; // For the 1th element
                // int elementSize = 4; // Size of INT in bytes (may vary based on the PLC configuration)
                // int writeOffset = elementIndex * elementSize;
                // Petal testP = new Petal(){ Position = (ushort)Random.Range(0,33), Velocity =  (ushort)Random.Range(0,33)};
                // byte[] petalBytes = StructToBytes(testP);
                // _tcAdsClient.WriteAny(arrayBaseHandle, petalBytes);
                // // TODO REF https://bkinfosys.beckhoff.com/english.php?content=../content/1033/tc3_ads.net/9407526667.html&id=
                // Debug.Log("Write goals[2] success");
                //
                // Petal readP = (Petal)_tcAdsClient.ReadAny(arrayBaseHandle, typeof(Petal));
                // Debug.Log($"Read goals[2] success. readP {readP.Position} {readP.Velocity}");

                
                WriteGoals();
                WriteModeAndError();
                WriteConfig();
                WriteMiraState();
                
                ReadMiraState();
                ReadGoals();
                ReadModeAndError();
                ReadConfig();

                // Read test
                // int varHandle = _tcAdsClient.CreateVariableHandle("ExtGV.miraState");
                // MiraStateS = (MiraState_s)_tcAdsClient.ReadAny(varHandle, typeof(MiraState_s));

                // Debug.Log("Writing activeError");
                // _tcAdsClient.WriteAny(varHandle, true);
                // Debug.Log("Writing success");

                //_miraState = (MiraState)test;

                // _miraState = ReadVariable<MiraState>("ExtGV.miraState");
                //PrintMiraState();
                //
                // ReadConstants();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Communication error: {ex.Message}");
            }

            yield return new WaitForSeconds(1); // Wait for 1 second before next send/receive
        }
    }

    private void WriteMiraState()
    {
        Debug.Log("Writing MiraState");
        int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.opsMode");
        _tcAdsClient.WriteAny(variableHandle, (short)MiraState.OpsModeE);
       
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.allState");
        _tcAdsClient.WriteAny(variableHandle, (short)MiraState.AllState);

        for (int i = 0; i < PetalsNum; i++)
        {
            variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.petal[{i+1}]");
            byte[] bytes = StructToBytes(MiraState.Petals[i]);
            _tcAdsClient.WriteAny(variableHandle, bytes);
            
            variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.petalError[{i+1}]");
            bytes = StructToBytes(MiraState.PetalError[i]);
            _tcAdsClient.WriteAny(variableHandle, bytes);
        }

       
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.sysError");
        byte[] byteArray = MiraState.SysError.Select(b => (byte)(b ? 1 : 0)).ToArray();
        _tcAdsClient.WriteAny(variableHandle, byteArray);
        
    }
    
    private void ReadMiraState()
    {
        int variableHandle;
        for (int i = 0; i < PetalsNum; i++)
        {
            variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.petal[{i+1}]");
            MiraState.Petals[i] = (PetalInfo)_tcAdsClient.ReadAny(variableHandle, typeof(PetalInfo));
            Debug.Log($"Read miraConfig.petal[{i}] success. {MiraState.Petals[i].Position} {MiraState.Petals[i].PetalState}");
            
            variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.petalError[{i+1}]");
            MiraState.PetalError[i] = (PetalErr)_tcAdsClient.ReadAny(variableHandle, typeof(PetalErr));
            Debug.Log($"Read miraConfig.petalError[{i}] success. {MiraState.PetalError[i].Stuck} {MiraState.PetalError[i].OverRun}");
        }

        for (int i = 0; i < NumAlarms; i++)
        {
            variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.sysError[{i+1}]");
            MiraState.SysError[i] = (bool)_tcAdsClient.ReadAny(variableHandle, typeof(bool));
            Debug.Log($"Read miraConfig.sysError[{i}] success. {MiraState.SysError[i]}");

        }
        
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.opsMode");
        MiraState.OpsModeE = (OpsMode_e)(int)_tcAdsClient.ReadAny(variableHandle, typeof(int));
        
        
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState.allState");
        MiraState.AllState = (AllState)(int)_tcAdsClient.ReadAny(variableHandle, typeof(int));
        
        
        // int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraState");
        // MiraState = (MiraState_s)_tcAdsClient.ReadAny(variableHandle, typeof(MiraState_s));
        // Debug.Log($"Read MiraState success");
        
    }
    
    
    // ----------------------------- CONFIG --------------------------------
    private void ReadConfig()
    {
        int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraConfig");
        MiraConfig = (MiraConfig)_tcAdsClient.ReadAny(variableHandle, typeof(MiraConfig));
        Debug.Log($"Read MiraConfig success. {MiraConfig}");
    }
    
    // write config
    private void WriteConfig()
    {
        int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.miraConfig");
        byte[] bytes = StructToBytes(MiraConfig);
        _tcAdsClient.WriteAny(variableHandle, bytes);
        Debug.Log($"Write MiraConfig success.");
    }


    // ----------------------------- MODE AND ERROR --------------------------------
    // NOTE: Enums are read in as ints then cast to the enum type
    private void ReadModeAndError()
    {
        int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.opsMode");
        OpsMode = (OpsMode_e)(int)_tcAdsClient.ReadAny(variableHandle, typeof(int));
        Debug.Log($"Read OpsMode success. {OpsMode}");
        
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.activeError");
        ActiveError = (bool)_tcAdsClient.ReadAny(variableHandle, typeof(bool));
        Debug.Log($"Read ActiveError success. {ActiveError}");
    }
    
    private void WriteModeAndError()
    {
        int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.opsMode");
        _tcAdsClient.WriteAny(variableHandle, (short)OpsMode);
        Debug.Log($"Write OpsMode success. {OpsMode}");
        
        variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.activeError");
       _tcAdsClient.WriteAny(variableHandle, ActiveError);
        Debug.Log($"Write ActiveError success. {ActiveError}");
    }
    
    // ----------------------------- GOALS --------------------------------
    private void ReadGoals()
    {
        for (int i = 0; i < PetalsNum; i++)
        {
            int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.goals[{i+1}]");
            Goals[i] = (Petal)_tcAdsClient.ReadAny(variableHandle, typeof(Petal));
            Debug.Log($"Read goals[{i}] success. {Goals[i].Position} {Goals[i].Velocity}");
        }
    }
    
    private void WriteGoals()
    {
        for (int i = 0; i < PetalsNum; i++)
        {
            int variableHandle = _tcAdsClient.CreateVariableHandle($"ExtGV.goals[{i+1}]");
            byte[] goalBytes = StructToBytes(Goals[i]);
            _tcAdsClient.WriteAny(variableHandle, goalBytes);
            Debug.Log($"Write goals[{i}] success. {Goals[i].Position} {Goals[i].Velocity}");
        }
    }
    
    // Helper method to convert Petal_s struct to bytes
    byte[] StructToBytes(Petal petal)
    {
        byte[] bytes = new byte[4];
        BitConverter.GetBytes(petal.Position).CopyTo(bytes, 0);
        BitConverter.GetBytes(petal.Velocity).CopyTo(bytes, 2);
        return bytes;
    }
    
    byte[] StructToBytes(PetalInfo petal)
    {
        byte[] bytes = new byte[4];
        BitConverter.GetBytes(petal.Position).CopyTo(bytes, 0);
        BitConverter.GetBytes((short)petal.PetalState).CopyTo(bytes, 2);
        return bytes;
    }
    
    byte[] StructToBytes(PetalErr petal)
    {
        byte[] bytes = new byte[sizeof(bool)*3];
        BitConverter.GetBytes(petal.Stuck).CopyTo(bytes, sizeof(bool)*0);
        BitConverter.GetBytes(petal.OverRun).CopyTo(bytes, sizeof(bool)*1);
        BitConverter.GetBytes(petal.Crash).CopyTo(bytes, sizeof(bool)*2);
        return bytes;
    }
    
    byte[] StructToBytes(MiraConfig config)
    {
        byte[] bytes = new byte[20];
        BitConverter.GetBytes(config.PetalOuterMaxPos).CopyTo(bytes, 0);
        BitConverter.GetBytes(config.PetalOuterMinPos).CopyTo(bytes, 2);
        BitConverter.GetBytes(config.PetalInnerMaxPos).CopyTo(bytes, 4);
        BitConverter.GetBytes(config.PetalInnerMinPos).CopyTo(bytes, 6);
        BitConverter.GetBytes(config.PetalOuterMaxSpeed).CopyTo(bytes, 8);
        BitConverter.GetBytes(config.PetalInnerMaxSpeed).CopyTo(bytes, 10);
        BitConverter.GetBytes(config.PetalOuterAccel).CopyTo(bytes, 12);
        BitConverter.GetBytes(config.PetalOuterDecel).CopyTo(bytes, 14);
        BitConverter.GetBytes(config.PetalInnerAccel).CopyTo(bytes, 16);
        BitConverter.GetBytes(config.PetalInnerDecel).CopyTo(bytes, 18);
        return bytes;
    }
    
    

    public T ReadVariable<T>(string varName)
    {
        int varHandle = _tcAdsClient.CreateVariableHandle(varName);
        return (T)_tcAdsClient.ReadAny(varHandle, typeof(T));
    }

    
    // Reads the constants from the PLC. Tested - working
    void ReadConstants()
    {
        //PetalsNum = ReadVariable<uint>("Constants_GV.PetalsNum");
        EffectorsNumModule = ReadVariable<uint>("Constants_GV.EffectorsNumModule");
        //NumAlarms = ReadVariable<uint>("Constants_GV.NumAlarms");
        MaxAccel = ReadVariable<int>("Constants_GV.MaxAccel");
        MaxDecel = ReadVariable<int>("Constants_GV.MaxDecel");
        PosnHysteresis = ReadVariable<int>("Constants_GV.PosnHysteresis");
        P1_HMvelocity = ReadVariable<int>("Constants_GV.P1_HMvelocity");
        ProfPosnModeOp = ReadVariable<sbyte>("Constants_GV.ProfPosnModeOp");
        ProfVelModeOp = ReadVariable<sbyte>("Constants_GV.ProfVelModeOp");
        HomingModeOp = ReadVariable<sbyte>("Constants_GV.HomingModeOp");
        PetalVel_Max = ReadVariable<float>("Constants_GV.PetalVel_Max");
        PetalVel_Min = ReadVariable<float>("Constants_GV.PetalVel_Min");
        PetalVelScaling = ReadVariable<float>("Constants_GV.PetalVelScaling");
    }

    void ListSymbols(TcAdsClient adsClient)
    {
        try
        {
            Debug.Log("Trying ListSymbols");
            ITcAdsSymbol5 root = (ITcAdsSymbol5)adsClient.ReadSymbolInfo("activeError");
            Debug.Log(root.DataType.ToString());
           // BrowseSymbols(root);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading symbols: " + ex.Message);
        }
    }

    // void BrowseSymbols(ITcAdsSymbol5 symbol)
    // {
    //     foreach (ITcAdsSymbol5 subSymbol in symbol.SubSymbols)
    //     {
    //         Console.WriteLine("Symbol Name: " + subSymbol.Name);
    //         BrowseSymbols(subSymbol); // Recursively browse sub-symbols
    //     }
    // }

    void OnDestroy()
    {
        if (_tcAdsClient != null)
        {
            _tcAdsClient.Dispose();
        }
    }
}
