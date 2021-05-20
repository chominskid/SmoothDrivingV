using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.UI;

namespace SmoothDrivingV
{
    public static unsafe class Memory
    {
        static Process process;
        static ulong baseAddress;
        static ulong endAddress;

        static ulong dmfvOffset = 0;
        static ulong gearRatiosOffset = 0;

        static ulong wheelsOffset = 0;
        static ulong wheelCountOffset = 0;
        static ulong wheelAngularVelocityOffset = 0;
        static ulong wheelFlagsOffset = 0;

        static ulong lightStatesOffset = 0;
        static ulong indicatorTimingOffset = 0;

        public static void Initialize()
        {
            process = Process.GetCurrentProcess();
            baseAddress = (ulong)process.MainModule.BaseAddress;
            endAddress = (ulong)process.MainModule.ModuleMemorySize + baseAddress;

            ulong address = FindPattern("\xF3\x0F\x5E\x93\xCC\x08\x00\x00\x0F\x2F\x15\x58\x0C\x92\x00\x73\x06\x45\x0F\x57\xC0", "xxxx????xxx????xxxxxx");
            dmfvOffset = address == 0 ? 0 : *(uint*)(address + 4);

            address = FindPattern("\xF3\x44\x0F\x5E\x84\x8B\x98\x08\x00\x00\x44\x8B\xE1\xF3\x0F\x10\x8B\x3C\x0B\x00\x00", "xxxxxx????xxxxxxx????");
            gearRatiosOffset = address == 0 ? 0 : *(uint*)(address + 6);

            address = FindPattern("\x3B\xB7\x48\x0B\x00\x00\x7D\x0D", "xx????xx");
            wheelCountOffset = address == 0 ? 0 : *(uint*)(address + 2);

            wheelsOffset = wheelCountOffset == 0 ? 0 : wheelCountOffset - 8;

            address = FindPattern("\x45\x0F\x57\xC9\xF3\x0F\x11\x83\x60\x01\x00\x00\xF3\x0F\x5C", "xxx?xxx???xxxxx");
            wheelAngularVelocityOffset = address == 0 ? 0 : *(uint*)(address + 8) + 12;

            address = FindPattern("\x75\x11\x48\x8b\x01\x8b\x88", "xxxxxxx");
            wheelFlagsOffset = address == 0 ? 0 : *(uint*)(address + 7);

            address = FindPattern("\xFD\x02\xDB\x08\x98\x00\x00\x00\x00\x48\x8B\x5C\x24\x30", "xxxxx????xxxxx");
            lightStatesOffset = address == 0 ? 0 : *(uint*)(address - 4) - 1;

            address = FindPattern("\x44\x0F\xB7\x91\xDC\x00\x00\x00\x0F\xB7\x81\xB0\x0A\x00\x00\x41\xB9\x01\x00\x00\x00\x44\x03\x15\x8C\x63\xDF\x01", "xxxx????xxx????xxxxxxxxx????");
            indicatorTimingOffset = address == 0 ? 0 : *(uint*)(address + 4);
        }

        [DllImport("ScriptHookV.dll", ExactSpelling = true, EntryPoint = "?createTexture@@YAHPEBD@Z")]
        public static extern int CreateTexture([MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport("ScriptHookV.dll", ExactSpelling = true, EntryPoint = "?drawTexture@@YAXHHHHMMMMMMMMMMMM@Z")]
        public static extern void DrawTexture(int id, int instance, int level, int time, float sizeX, float sizeY, float centerX, float centerY, float posX, float posY, float rotation, float scaleFactor, float colorR, float colorG, float colorB, float colorA);

        public static List<float> GetGearRatios(this Vehicle vehicle)
        {
            if (gearRatiosOffset == 0)
            {
                return null;
            }

            ulong vehicleAddress = (ulong)vehicle.MemoryAddress;
            List<float> returnList = new List<float>();

            for (ulong i = 0; i <= (ulong)vehicle.HighGear; i++)
            {
                returnList.Add(*(float*)(vehicleAddress + gearRatiosOffset + i * sizeof(float)));
            }

            return returnList;
        }

        public static void SetGearRatios(this Vehicle vehicle, ICollection<float> gearRatios)
        {
            if (gearRatiosOffset == 0 || gearRatios.Count != vehicle.HighGear + 1)
            {
                return;
            }

            ulong vehicleAddress = (ulong)vehicle.MemoryAddress;

            for (int i = 0; i < gearRatios.Count; i++)
            {
                *(float*)(vehicleAddress + gearRatiosOffset + (ulong)i * sizeof(float)) = gearRatios.ElementAt(i);
            }
        }

        public static int GetWheelCount(this Vehicle vehicle)
        {
            if (wheelCountOffset == 0)
            {
                return 0;
            }

            return *(int*)((ulong)vehicle.MemoryAddress + wheelCountOffset);
        }

        public static float GetWheelRadius(this Vehicle vehicle, uint index)
        {
            if (wheelsOffset == 0)
            {
                return 0.0f;
            }

            return *(float*)(*(ulong*)(*(ulong*)((ulong)vehicle.MemoryAddress + wheelsOffset) + index * 0x8) + 0x110);
        }

        public static float GetWheelAngularVelocity(this Vehicle vehicle, uint index)
        {
            if (wheelsOffset == 0 || wheelAngularVelocityOffset == 0)
            {
                return 0.0f;
            }

            return -*(float*)(*(ulong*)(*(ulong*)((ulong)vehicle.MemoryAddress + wheelsOffset) + index * 0x8) + wheelAngularVelocityOffset);
        }

        public static bool GetIndicatorFlash(this Vehicle vehicle)
        {
            if (indicatorTimingOffset == 0 || !vehicle.IsEngineRunning)
            {
                return false;
            }

            uint a = *(uint*)((ulong)vehicle.MemoryAddress + indicatorTimingOffset);
            a += (uint)Game.GameTime;
            a = a >> 9;
            a = a & 1;
            return a == 1;
        }

        public static float GetDriveMaxFlatVelocity(this Vehicle vehicle)
        {
            if (dmfvOffset == 0)
            {
                return 0.0f;
            }

            return *(float*)((ulong)vehicle.MemoryAddress + dmfvOffset);
        }

        public static void SetDriveMaxFlatVelocity(this Vehicle vehicle, float driveMaxFlatVelocity)
        {
            if (dmfvOffset != 0)
            {
                *(float*)((ulong)vehicle.MemoryAddress + dmfvOffset) = driveMaxFlatVelocity;
            }
        }

        public static uint GetLightStates(this Vehicle vehicle)
        {
            if (lightStatesOffset == 0)
            {
                return 0;
            }

            return *(uint*)((ulong)vehicle.MemoryAddress + lightStatesOffset);
        }

        public static ushort GetWheelFlags(this Vehicle vehicle, uint index)
        {
            if (wheelsOffset == 0 || wheelFlagsOffset == 0)
            {
                return 0;
            }

            return *(ushort*)(*(ulong*)(*(ulong*)((ulong)vehicle.MemoryAddress + wheelsOffset) + index * 0x8) + wheelFlagsOffset);
        }

        public static void SetWheelFlags(this Vehicle vehicle, uint index, ushort flags)
        {
            if (wheelsOffset == 0 || wheelFlagsOffset == 0)
            {
                return;
            }

            *(ushort*)(*(ulong*)(*(ulong*)((ulong)vehicle.MemoryAddress + wheelsOffset) + index * 0x8) + wheelFlagsOffset) = flags;
        }

        public static string PatternToString(string pattern, string mask = "")
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < pattern.Length; i++)
            {
                if (mask.Length > 0 && mask[i] == '?')
                {
                    stringBuilder.Append("?? ");
                }
                else
                {
                    stringBuilder.Append(Convert.ToString((byte)pattern[i], 16).PadLeft(2, '0').ToUpper() + " ");
                }
            }

            return stringBuilder.ToString();
        }

        public static ulong FindPattern(string pattern, string mask = "")
        {
            if (mask.Length > 0 && pattern.Length != mask.Length)
            {
                Logger.WriteToLog("Failed to find pattern '" + PatternToString(pattern, mask) + "', arguments invalid.");
                return 0;
            }

            for (ulong a = baseAddress; a < endAddress; a++)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (*(byte*)(a + (ulong)i) != pattern[i] && (mask.Length == 0 || mask[i] == 'x'))
                    {
                        break;
                    }
                    else if (i == pattern.Length - 1)
                    {
                        Logger.WriteToLog("Pattern '" + PatternToString(pattern, mask) + "' located successfully (address GTA5.exe + " + Convert.ToString((long)(a - baseAddress), 16).ToUpperInvariant() + ")");
                        return a;
                    }
                }
            }

            Logger.WriteToLog("Failed to find pattern '" + PatternToString(pattern, mask) + "'.");

            return 0;
        }

        public class MemoryPatcher
        {
            public string Name { get; set; }
            public int Status { get; set; } //-1: Error, 1: Standby, 2: Active
            public ulong Address { get; set; }
            public ulong Length { get; set; }
            public byte[] Code { get; set; }

            public MemoryPatcher(string name, string pattern, string code, string mask = "")
            {
                Name = name;
                Length = (ulong)code.Length;
                Code = code.Select(x => (byte)x).ToArray();
                Status = 0;
                Address = FindPattern(pattern, mask);

                if (Address == 0) { Status = -1; Logger.WriteToLog("Memory patcher '" + name + "' initialization failed!"); }
                else
                {
                    Status = 1;
                    Logger.WriteToLog("Memory patcher '" + name + "' initialized successfully (address GTA5.exe + " + Convert.ToString((long)(Address - baseAddress), 16).ToUpperInvariant() + " with " + Length + " bytes).");
                }
            }

            public void Apply()
            {
                if (Status == 1)
                {
                    try
                    {
                        byte[] temp = Code.Select(x => x).ToArray();

                        for (ulong i = 0; i < Length; i++)
                        {
                            temp[i] = *(byte*)(i + Address);
                            *(byte*)(i + Address) = Code[i];
                        }

                        Code = temp;
                        Status = 2;
                    }
                    catch (Exception exception)
                    {
                        Logger.WriteToLog("Memory patcher '" + Name + "' application failed! Error: " + exception.Message);
                    }
                }
            }

            public void Revert()
            {
                if (Status == 2)
                {
                    try
                    {
                        byte[] temp = Code.Select(x => x).ToArray();

                        for (ulong i = 0; i < Length; i++)
                        {
                            temp[i] = *(byte*)(i + Address);
                            *(byte*)(i + Address) = Code[i];
                        }

                        Code = temp;
                        Status = 1;
                    }
                    catch (Exception exception)
                    {
                        Logger.WriteToLog("Memory patcher '" + Name + "' revert failed! Error: " + exception.Message);
                    }
                }
            }
        }
    }
}
