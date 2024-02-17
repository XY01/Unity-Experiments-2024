using System;
using TwinCAT.TypeSystem;

namespace PelicanStudios.Mira
{
    // Enums
    public enum OpsMode_e
    {
        Standby = 0,
        Manual = 1,
        Run = 2
    }

    public enum AllState
    {
        Initializing = 0,
        Waiting = 1,
        Ready = 2,
        Busy = 3,
        Done = 4,
        Error = 5
    }

    public enum PetalState
    {
        NotReady = 0,
        Standby = 1,
        MovingIn = 2,
        MovingOut = 3,
        InPosn = 4,
        Error = 5
    }


    // Structs
    [Serializable]
    public struct MiraState_s
    {
        public OpsMode_e OpsModeE;
        public AllState AllState;
        public PetalInfo[] Petals; // Size should be PetalsNum
        public bool[] SysError; // Size should be NumAlarms
        public PetalErr[] PetalError; // Size should be PetalsNum
    }
    [Serializable]
    public struct MiraConfig
    {
        public ushort PetalOuterMaxPos;
        public ushort PetalOuterMinPos;
        public ushort PetalInnerMaxPos;
        public ushort PetalInnerMinPos;
        public ushort PetalOuterMaxSpeed;
        public ushort PetalInnerMaxSpeed;
        public ushort PetalOuterAccel;
        public ushort PetalOuterDecel;
        public ushort PetalInnerAccel;
        public ushort PetalInnerDecel;
    }

    [Serializable]
    public struct Petal
    {
        public ushort Position;
        public ushort Velocity;
    }
    [Serializable]
    public struct PetalInfo
    {
        public ushort Position;
        public PetalState PetalState;
    }
    [Serializable]
    public struct PetalErr
    {
        public bool Stuck;
        public bool OverRun;
        public bool Crash;
    }
}