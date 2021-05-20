using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace SmoothDrivingV
{
    public struct Wheel
    {
        public float angularVelocity;
        public float radius;
        public float forwardSpeed;
        public ushort wheelFlags;
        public bool isWheelPowered;
        public bool isWheelSteered;

        public Wheel(Vehicle vehicle, uint index)
        {
            angularVelocity = vehicle.GetWheelAngularVelocity(index);
            radius = vehicle.GetWheelRadius(index);
            forwardSpeed = radius * angularVelocity;
            wheelFlags = vehicle.GetWheelFlags(index);
            isWheelPowered = (wheelFlags & 16) == 16;
            isWheelSteered = (wheelFlags & 8) == 8;
        }
    }

    public class Main : Script
    {
        public Main()
        {
            Logger.InitializeLog();
            Logger.WriteToLog("Starting SmoothDrivingV initialization...");
            UI.Initialize();

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAbort;

            Memory.Initialize();

            runtimePatchers.AddRange(new Memory.MemoryPatcher[]
            {
                new Memory.MemoryPatcher("Steering Center", "\x44\x89\xBB\xB4\x09\x00\x00\x8B\x0D\xC6\xF8\xE6\x00\x0F\x57\xC0\x81\xF9\xFF\xFF\x00\x00\x74\x60", "\x90\x90\x90\x90\x90\x90\x90", "xxx????xx????xxxxx????x?"),
                new Memory.MemoryPatcher("Fuel", "\x0F\x83\xBD\x00\x00\x00\x44\x0F\x2F\xA1\x00\x09\x00\x00\xF3\x44\x0F\x5C\xD0", "\xE9\xBE\x00\x00\x00\x90", "xxxxxxxxxx????xxxxx"), //Stops the car from jerking backwards and sparking at low fuel
            });

            steeringPatchers.AddRange(new Memory.MemoryPatcher[]
            {
                new Memory.MemoryPatcher("Steering Part 1", "\xF3\x0F\x11\x8B\xAC\x09\x00\x00\xF3\x0F\x10\x83\xB0\x09\x00\x00\xF3\x0F\x58\x83\xAC\x09\x00\x00", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????xxxx????xxxx????"), //Simultaneous
                new Memory.MemoryPatcher("Steering Part 2", "\xF3\x0F\x11\x83\xAC\x09\x00\x00\x73\x06\x45\x0F\x28\xC4\xEB\x13\xF3\x44\x0F\x10\x05\x50\xE5", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????x?xxxxx?xxxxx??"), //Simultaneous
                new Memory.MemoryPatcher("Steering Part 3", "\xF3\x44\x0F\x11\x83\xAC\x09\x00\x00\x4C\x8B\xCB\x45\x8A\xC6\x48\x8B\xD5\x49\x8B\xCF\xF3\x44", "\x90\x90\x90\x90\x90\x90\x90\x90\x90", "xxxxx????xxxxxxxxxxxxxx"), //Simultaneous
            
                new Memory.MemoryPatcher("Motorcycle Steering Part 1", "\xF3\x0F\x11\x8B\xAC\x09\x00\x00\x73\x06\x41\x0F\x28\xCC\xEB\x08\x0F\x2F\xCA", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????x?xxxxx?xxx"),
                new Memory.MemoryPatcher("Motorcycle Steering Part 2", "\xF3\x0F\x11\x8B\xAC\x09\x00\x00\xF3\x0F\x10\x4D\x7F\x41\x0F\x2F\xCC\x73\x06", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????xxxx?xxxxx?"),
                new Memory.MemoryPatcher("Motorcycle Steering Part 3", "\xF3\x0F\x11\x83\xAC\x09\x00\x00\x73\x06\x41\x0F\x28\xF4\xEB\x10\xF3\x0F\x10", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????x?xxxxx?xxx"),
                new Memory.MemoryPatcher("Motorcycle Steering Part 4", "\xF3\x0F\x11\xB3\xAC\x09\x00\x00\x8A\x88\xA6\x07\x00\x00\x84\xC9\x78\x15\x48", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????xx????xxx?x"),
            });

            vehiclePatchers.AddRange(new Memory.MemoryPatcher[]
            {
                new Memory.MemoryPatcher("Throttle 1", "\xF3\x0F\x11\xB3\xBC\x09\x00\x00\x89\xAB\xC0\x09\x00\x00\x8B\x83\xE8\x0B\x00\x00\x83\xE8\x06\x41\x3B\xC6", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????xx????xx????xxxxxx"), //During normal driving
                new Memory.MemoryPatcher("Throttle 2", "\x89\xAB\xBC\x09\x00\x00\xE9\xE7\xFE\xFF\xFF\x48\x8B\xC4", "\x90\x90\x90\x90\x90\x90", "xx????x????xxx"), //During braking
                new Memory.MemoryPatcher("Throttle 3", "\xF3\x44\x0F\x11\x8B\xBC\x09\x00\x00\xF3\x44\x0F\x11\x93\xC0\x09\x00\x00\xE9\x53\xFF\xFF\xFF\x41\x0F\x2F\xF8", "\x90\x90\x90\x90\x90\x90\x90\x90\x90", "xxxxx????xxxxx????x????xxxx"), //During burnout
                
                new Memory.MemoryPatcher("Brake 1", "\x89\xAB\xC0\x09\x00\x00\x8B\x83\xE8\x0B\x00\x00\x83\xE8\x06\x41\x3B\xC6\x77\x07", "\x90\x90\x90\x90\x90\x90", "xx????xx????xxxxxxx?"), //During normal driving
                new Memory.MemoryPatcher("Brake 2", "\xF3\x0F\x11\xB3\xC0\x09\x00\x00\x89\xAB\xBC\x09\x00\x00\xE9\xE7\xFE\xFF\xFF", "\x90\x90\x90\x90\x90\x90\x90\x90", "xxxx????xx????x????"), //During braking
                new Memory.MemoryPatcher("Brake 3", "\xF3\x44\x0F\x11\x93\xC0\x09\x00\x00\xE9\x53\xFF\xFF\xFF\x41\x0F\x2F\xF8\x72\x28", "\x90\x90\x90\x90\x90\x90\x90\x90\x90", "xxxxx????x????xxxxx?"), //During burnout
            });

            List<string> configLines = new List<string>();
            configLines.AddRange(File.ReadAllLines(Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/Driving.ini"));
            configLines.AddRange(File.ReadAllLines(Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/Controls.ini"));
            configLines.AddRange(File.ReadAllLines(Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/UI.ini"));

            enableAdvancedGearbox = Config.ReadBool(configLines, "EnableAdvancedGearbox", true);

            useManualGearbox = Config.ReadBool(configLines, "UseManualGearbox", false);

            enableSmoothSteering = Config.ReadBool(configLines, "EnableSmoothSteering", true);

            if (enableAdvancedGearbox)
            {
                vehiclePatchers.AddRange(new Memory.MemoryPatcher[]
                {
                    new Memory.MemoryPatcher("Clutch 1", "\xF3\x0F\x11\x43\x4C\x0F\xB7\x43\x04\x40\x84\xC5\x74\x14", "\x90\x90\x90\x90\x90", "xxxx?xxx?xxxx?"),
                    new Memory.MemoryPatcher("Clutch 2", "\xC7\x43\x4C\xCD\xCC\xCC\x3D\xE9\xFF\x05\x00\x00\x44\x0F\x2F\xE7", "\x90\x90\x90\x90\x90\x90\x90", "xxx????x????xxxx"),
                    new Memory.MemoryPatcher("Clutch 3", "\xF3\x0F\x11\x47\x4C\x48\x8B\x06\x44\x0F\x28\xC7\x48\x8B\xCE", "\x90\x90\x90\x90\x90", "xxxx?xxxxxxxxxx"),
                    new Memory.MemoryPatcher("Clutch 4", "\xC7\x43\x4C\xCD\xCC\xCC\x3D\x44\x89\x6B\x6C\x44\x89\x73\x68", "\x90\x90\x90\x90\x90\x90\x90", "xxx????xxx?xxx?"),
                    new Memory.MemoryPatcher("Clutch 5", "\xC7\x43\x4C\xCD\xCC\xCC\x3D\x66\x44\x89\x43\x04\x66\x89\x13\x44\x89\x73\x68\xEB\x0A", "\x90\x90\x90\x90\x90\x90\x90", "xx?????xxxx?xxxxxx?x?"), //Stops the RPM from dropping down to idle during low throttle + low-ish speed
                
                    new Memory.MemoryPatcher("Upshift", "\x66\x89\x0B\x8D\x46\x04\x66\x89\x43\x04\x0F\xBE\x8F\x61\x0C\x00\x00", "\x90\x90\x90", "xxxxx?xxx?xxx????"),
                    new Memory.MemoryPatcher("Downshift", "\x66\xFF\x0B\x66\x39\x33\x7E\x18\x48\x0F\xBF\x03\x41\x0F\x28\xC2", "\x90\x90\x90", "xxxxxxx?xxxxxxxx"),
                    new Memory.MemoryPatcher("Downshift Jump", "\x72\xE0\x66\x44\x89\x4B\x04\xC7\x43\x4C\xCD\xCC\xCC\x3D\x44\x89\x73\x68\xE9\x85\x03\x00\x00", "\x90\x90", "x?xxxx?xxx????xxx?x????"), //To prevent infinite loop on downshift
                    new Memory.MemoryPatcher("Brake Downshift", "\x66\xFF\x0B\x66\x39\x33\x0F\x8E\xBC\x00\x00\x00\x48\x0F\xBF\x03", "\x90\x90\x90", "xxxxxxxx????xxxx"), //When braking hard
                    new Memory.MemoryPatcher("Brake Downshift Jump", "\x72\xDC\xE9\x9F\x00\x00\x00\x41\x83\xF8\x0C\x0F\x84\xAA\x00\x00\x00", "\x90\x90", "x?x????xxx?xx????"), //To prevent infinite loop on brake downshift
                    new Memory.MemoryPatcher("Shift Reverse", "\x66\x44\x89\x2B\xC7\x43\x4C\xCD\xCC\xCC\x3D\xE9\x8F\x05\x00\x00", "\x90\x90\x90\x90", "xxxxxx?????x????"),
                    new Memory.MemoryPatcher("Shift Forward", "\x66\x89\x33\x44\x0F\x28\x74\x24\x30\x44\x0F\x28\x7C\x24\x20", "\x90\x90\x90", "xxxxxxxx?xxxxx?"),
                    new Memory.MemoryPatcher("Shift Handbrake", "\x66\x89\x13\x44\x89\x73\x68\xEB\x0A\x44\x0F\x2E\xEF", "\x90\x90\x90", "xxxxxx?x?xxxx"), //Idk what this does but it seems to downshift upon pressing the handbrake
                });

                targetRPMFallRate = Config.ReadFloat(configLines, "TargetRPMFallRate", 0.15f);
                targetRPMRiseRate = Config.ReadFloat(configLines, "TargetRPMRiseRate", 0.05f);
            }

            foreach (Memory.MemoryPatcher m in runtimePatchers)
            {
                m.Apply();
            }

            cruiseMinSpeed = Config.ReadFloat(configLines, "CruiseMinimumSpeed", 5.56f);
            cruiseTgtAccFactor = Config.ReadFloat(configLines, "CruiseTargetAccelerationMultiplier", 0.5f);
            cruiseMaxAcceleration = Config.ReadFloat(configLines, "CruiseMaximumAcceleration", 5.0f);

            enableAutoFollow = Config.ReadBool(configLines, "EnableAutoFollow", true);

            if (enableAutoFollow)
            {
                cruiseFollowingDistance = Config.ReadFloat(configLines, "CruiseFollowingDistance", 1.2f);
            }

            enableTractionControl = Config.ReadBool(configLines, "EnableTractionControl", true);

            minForwardAcceleration = Config.ReadFloat(configLines, "MinForwardAcceleration", 2.0f);
            maxForwardAcceleration = Config.ReadFloat(configLines, "MaxForwardAcceleration", 2.8f);
            targetForwardAcceleration = 0.5f * (minForwardAcceleration + maxForwardAcceleration);

            defaultBrakeDeceleration = Config.ReadFloat(configLines, "TargetBrakeDeceleration", 2.5f);

            standardShiftDuration = Config.ReadFloat(configLines, "StandardShiftDuration", 1.2f);

            enableFuelScript = Config.ReadBool(configLines, "EnableFuelScript", true);

            fuelConsumptionModifier = Config.ReadFloat(configLines, "FuelConsumptionModifier", 0.00038f);

            fuelPumpRate = Config.ReadFloat(configLines, "FuelPumpRate", 0.83333f);

            throttleChangeDelay = Config.ReadFloat(configLines, "ThrottleChangeDelay", 2.5f);

            KeysConverter keysConverter = new KeysConverter();

            doubleTapThreshold = Config.ReadFloat(configLines, "DoubleTapMaxDelay", 0.2f);

            AIGearboxManager.maxIndex = Config.ReadInt32(configLines, "MaxAIGearboxCalculationsPerFrame", 20);

            showGearConfigNotification = Config.ReadBool(configLines, "ShowGearboxConfigurationNotification", false);

            leftIndicatorKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "LeftIndicatorKey", "Left"));
            leftIndicatorKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "LeftIndicatorKeyModifier", "None"));

            rightIndicatorKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "RightIndicatorKey", "Right"));
            rightIndicatorKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "RightIndicatorKeyModifier", "None"));

            hazardKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "HazardKey", "Down"));
            hazardKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "HazardKeyModifier", "None"));

            engineKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "EngineKey", "Z"));
            engineKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "EngineKeyModifier", "Shift"));
            enableEngineControl = engineKey != Keys.None;
            enableKeyTurnAnimation = Config.ReadBool(configLines, "EnableKeyTurnAnimation", true);

            downShiftKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "DownShiftKey", "3"));
            downShiftKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "DownShiftKeyModifier", "None"));

            upShiftKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "UpShiftKey", "4"));
            upShiftKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "UpShiftKeyModifier", "None"));

            cruiseKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseControlKey", "T"));
            cruiseKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseControlKeyModifier", "Shift"));

            cruiseResumeKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseControlResumeKey", "Y"));
            cruiseResumeKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseControlResumeKeyModifier", "Shift"));

            cruiseFasterKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseFasterKey", "U"));
            cruiseFasterKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseFasterKeyModifier", "Shift"));

            cruiseSlowerKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseSlowerKey", "Y"));
            cruiseSlowerKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "CruiseSlowerKeyModifier", "Shift"));

            raceModeKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "RaceModeKey", "LMenu"));
            raceModeKeyModifier = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "RaceModeKeyModifier", "None"));

            directionSwitchKey = (Keys)keysConverter.ConvertFromInvariantString(Config.ReadString(configLines, "DirectionSwitchKey", "Space"));

            steeringSensitivity = Config.ReadFloat(configLines, "SteeringSensitivity", 1.4f);
            steeringInputRise = Config.ReadFloat(configLines, "SteeringInputRise", 10.0f);
            steeringInputFall = Config.ReadFloat(configLines, "SteeringInputFall", 25.0f);
            steeringSpeedCompensation = Config.ReadFloat(configLines, "SteeringSpeedCompensation", 0.036f);
            steeringInputSmoothing = Config.ReadFloat(configLines, "SteeringInputSmoothing", 0.04f);
            steeringCentering = Config.ReadFloat(configLines, "SteeringCentering", 0.16f);

            string[] grConfigFiles = Directory.GetFiles(Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/Gearbox/", "*.ini");

            foreach (string f in grConfigFiles)
            {
                string name = f.Split('/').Last().Replace(".ini", "");
                configLines.Clear();
                configLines.AddRange(File.ReadAllLines(f));

                string[] models = Config.ReadStrings(configLines, "VehicleModels", new string[] { "," }, new string[] { });
                if (models == null || models.Length == 0) continue;
                int[] modelHashes = models.Select(x => Game.GenerateHash(x.Trim())).ToArray();

                float driveMaxFlatVelocity = Config.ReadFloat(configLines, "DriveMaxFlatVelocity", -1.0f);
                if (driveMaxFlatVelocity <= 0.0f) continue;

                float[] gearRatios = Config.ReadStrings(configLines, "GearRatios", new string[] { "," }, new string[] { }).Select(x => float.Parse(x.Trim().Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                if (gearRatios == null || gearRatios.Length == 0) continue;
                if (gearRatios[0] > 0.0f) gearRatios[0] = -gearRatios[0];

                gearRatioConfigurations.Add(new GearRatioConfiguration(name, modelHashes, driveMaxFlatVelocity, Config.ReadFloat(configLines, "SpeedLimiter", 0.0f) / 3.6f, gearRatios));
            }
        }

        //General
        public Vehicle vehicle;
        private Vehicle previousVehicle;
        public static bool inVehicleLastFrame = false;
        public static bool isElectric = false;
        private bool engineState = false;
        private bool enableEngineControl = true;
        private float previousRPM = 0.0f;

        List<Memory.MemoryPatcher> runtimePatchers = new List<Memory.MemoryPatcher>(); //Active throughout entire script runtime
        List<Memory.MemoryPatcher> steeringPatchers = new List<Memory.MemoryPatcher>(); //Active only during non-mouse steering
        List<Memory.MemoryPatcher> vehiclePatchers = new List<Memory.MemoryPatcher>(); //Active only when in a vehicle

        //Controls
        private float doubleTapThreshold = 0.2f;

        private Keys engineKey = Keys.Z;
        private Keys engineKeyModifier = Keys.Shift;
        private bool engineKeyDown = false;
        private bool engineKeyDownLastFrame = false;
        private bool enableKeyTurnAnimation = true;

        private Keys downShiftKey = Keys.D3;
        private Keys downShiftKeyModifier = Keys.None;
        private bool downShiftKeyDown = false;
        private bool downShiftKeyDownLastFrame = false;

        private Keys upShiftKey = Keys.D4;
        private Keys upShiftKeyModifier = Keys.None;
        private bool upShiftKeyDown = false;
        private bool upShiftKeyDownLastFrame = false;

        private Keys cruiseKey = Keys.T;
        private Keys cruiseKeyModifier = Keys.Shift;
        private bool cruiseKeyDown = false;
        private bool cruiseKeyDownLastFrame = false;

        private Keys cruiseResumeKey = Keys.Y;
        private Keys cruiseResumeKeyModifier = Keys.Shift;
        private bool cruiseResumeKeyDown = false;
        private bool cruiseResumeKeyDownLastFrame = false;

        private Keys cruiseFasterKey = Keys.U;
        private Keys cruiseFasterKeyModifier = Keys.Shift;
        private bool cruiseFasterKeyDown = false;

        private Keys cruiseSlowerKey = Keys.Y;
        private Keys cruiseSlowerKeyModifier = Keys.Shift;
        private bool cruiseSlowerKeyDown = false;

        private Keys raceModeKey = Keys.Menu;
        private Keys raceModeKeyModifier = Keys.None;
        private bool raceModeKeyDown = false;
        private bool raceModeKeyDownLastFrame = false;
        private bool raceModeActive = false;

        private Keys directionSwitchKey = Keys.Space;
        private bool directionSwitchKeyDown = false;

        private Keys leftIndicatorKey = Keys.Left;
        private Keys leftIndicatorKeyModifier = Keys.None;
        private bool leftIndicatorKeyDown = false;
        private bool leftIndicatorKeyDownLastFrame = false;

        private Keys rightIndicatorKey = Keys.Right;
        private Keys rightIndicatorKeyModifier = Keys.None;
        private bool rightIndicatorKeyDown = false;
        private bool rightIndicatorKeyDownLastFrame = false;

        private Keys hazardKey = Keys.Down;
        private Keys hazardKeyModifier = Keys.None;
        private bool hazardKeyDown = false;
        private bool hazardKeyDownLastFrame = false;

        private int indicatorState = 0; //0: Off, 1: Left, 2: Right, 3: Hazard

        //Gearbox
        private struct GearRatioConfiguration
        {
            public string name;
            public int[] modelHashes;
            public float driveMaxFlatVelocity;
            public float speedLimiter;
            public float[] gearRatios;

            public GearRatioConfiguration(string name, int[] modelHashes, float driveMaxFlatVelocity, float speedLimiter, float[] gearRatios)
            {
                this.name = name;
                this.modelHashes = modelHashes;
                this.driveMaxFlatVelocity = driveMaxFlatVelocity;
                this.speedLimiter = speedLimiter;
                this.gearRatios = gearRatios;
            }
        }

        private List<GearRatioConfiguration> gearRatioConfigurations = new List<GearRatioConfiguration>();

        public static bool enableAdvancedGearbox = true;
        public static bool useManualGearbox = true;
        private bool hasGearboxConfig = false;
        private List<float> baseGearRatios;
        private float[] tempGearRatios;

        private bool showGearConfigNotification = true;

        private int direction = 1; //-1: Reverse, 1: Forward
        private float shiftWaitTimer = 0.0f;
        private float shiftDelay = 1.5f;

        private bool shifting = false;
        private float shiftTimer = 0.0f;
        private float targetRPMFallRate = 0.15f;
        private float targetRPMRiseRate = 0.05f;
        private float standardShiftDuration = 1.2f;
        private float upShiftDuration = 0.0f;
        private float downShiftDuration = 0.0f;

        private float driveMaxFlatVelocity = 0.0f;
        public static float speedLimiter = 0.0f;

        private int topGear = 0;
        private int lastGear = 0;
        private int currentGear = 0;

        private float targetRPM = 0.0f;
        private float decelUpShiftTimer = 0.0f;
        private bool decelMode = false;

        //Fuel
        private bool enableFuelScript = true;
        //private float fuelLevelLastFrame = 0.0f;
        //private float averageRange = 0.0f;
        private float fuelTankVolume = 0.0f;
        private float fuelConsumptionModifier = 0.00038f;
        private float fuelPumpRate = 0.83333f;
        private float jerryCanDrainTimer = 0.0f;
        private float jerryCanDrainRate = 225.0f;
        private bool playedFuelFinishSound = false;

        private Model[] fuelPumpModels = new Model[]
        {
            new Model("prop_gas_pump_1a"),
            new Model("prop_gas_pump_1b"),
            new Model("prop_gas_pump_1c"),
            new Model("prop_gas_pump_1d"),
            new Model("prop_vintage_pump"),
            new Model("prop_gas_pump_old2"),
            new Model("prop_gas_pump_old3"),
        };

        //Speed stuff
        private float forwardSpeed = 0.0f;
        private float forwardSpeedLastFrame = 0.0f;
        private float forwardAcceleration = 0.0f;

        private float forwardDriveWheelSpeedLastFrame = 0.0f;
        private float forwardDriveAcceleration = 0.0f;

        private float velocity = 0.0f;
        private float velocityLastFrame = 0.0f;
        private float velocityAcceleration = 0.0f;

        //private decimal odometer = 0.0m;

        //Steering
        public bool enableSmoothSteering = true;

        private float steering = 0.0f;
        private float steeringSensitivity = 1.4f;

        private float steeringInputRise = 10.0f;
        private float steeringInputFall = 25.0f;
        private float steeringInputSmoothing = 0.04f;
        private float steeringSpeedCompensation = 0.036f;
        private float steeringCentering = 0.16f;

        private float steeringInput = 0.0f;

        private float indicatorCutoffArmThreshold = 0.5f;
        private float indicatorCutoffThreshold = 0.15f;
        private bool armIndicatorCutoff = false;

        //Throttle
        private float throttle = 0.0f;
        private float maxThrottle = 1.0f;
        private bool burnoutLastFrame = false;
        private bool lightThrottleLastFrame = false;

        private bool enableTractionControl = true;
        private float tcsTimer = 0.0f;
        private float tcsDelay = 0.2f;
        private float tcsFactor = 1.0f;

        private float minForwardAcceleration = 2.0f;
        private float targetForwardAcceleration = 2.4f;
        private float maxForwardAcceleration = 2.8f;

        private float throttleChangeTimer = 0.0f;
        private float throttleChangeDelay = 2.5f;

        private float accelerateDoubleTapTimer = 0.0f;
        private bool accelerateDoubleTap = false;

        //Cruise Control
        private bool cruiseControlActive = false;

        private float cruiseSpeed = 0.0f;
        private float cruiseMinSpeed = 5.56f;

        private float cruiseThrottle = 0.0f;
        private float cruiseMaxAcceleration = 5.0f;
        private float cruiseTgtAccFactor = 0.5f;
        private float cruiseAccCorrFactor = 1.0f;

        private bool enableAutoFollow = true;
        private float cruiseFollowingDistance = 1.2f;

        private bool preventSpeedChange = false;

        //Brake
        private float brake = 0.0f;
        private bool lightBrakeLastFrame = false;

        private float defaultBrakeDeceleration = 4.0f;

        private float brakeDoubleTapTimer = 0.0f;
        private bool brakeDoubleTap = false;

        //TO BE IMPLEMENTED
        /*
        private Model[] brakeTargetModels = new Model[]
        {
            new Model("prop_sign_road_01a"),
            new Model("prop_traffic_01a"),
        };
        */

        //TO BE IMPLEMENTED
        /*
        private MaterialHash[] autoBrakeBlacklist = new MaterialHash[]
        {
            MaterialHash.Tarmac,
            MaterialHash.TarmacPainted,
            MaterialHash.TarmacPothole,
            MaterialHash.ConcretePavement,
            MaterialHash.ConcretePothole,
            MaterialHash.Grass,
            MaterialHash.GrassLong,
            MaterialHash.GrassShort,
            MaterialHash.SandCompact,
            MaterialHash.SandDryDeep,
            MaterialHash.SandLoose,
            MaterialHash.SandstoneBrittle,
            MaterialHash.SandstoneSolid,
            MaterialHash.SandTrack,
            MaterialHash.SandUnderwater,
            MaterialHash.SandWet,
            MaterialHash.SandWetDeep,
        };
        */

        private void OnTick(object sender, EventArgs eventArgs)
        {
            Time.Update(Game.FPS);
            Ped playerPed = Game.Player.Character;
            vehicle = playerPed.CurrentVehicle;

            if (vehicle != null && vehicle.Exists() && (
                    vehicle.Model.IsCar
                    || vehicle.Model.IsBike
                    || vehicle.Model.IsQuadBike
                    ) && playerPed.IsSittingInVehicle() && !vehicle.Model.IsTank)
            {
                engineState = vehicle.IsEngineRunning;

                if (!inVehicleLastFrame || previousVehicle != vehicle)
                {
                    throttle = 0.0f;
                    steering = vehicle.SteeringScale;
                    currentGear = vehicle.CurrentGear;
                    direction = 1;
                    previousVehicle = vehicle;
                    upShiftDuration = standardShiftDuration / vehicle.HandlingData.ClutchChangeRateScaleUpShift;
                    downShiftDuration = standardShiftDuration / vehicle.HandlingData.ClutchChangeRateScaleDownShift;
                    isElectric = vehicle.Model.IsElectricVehicle;

                    foreach (Memory.MemoryPatcher m in vehiclePatchers)
                    {
                        m.Apply();
                    }

                    if (enableEngineControl)
                    {
                        Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, engineState, true, true);
                    }

                    hasGearboxConfig = false;
                    int modelHash = vehicle.Model.Hash;

                    foreach (GearRatioConfiguration g in gearRatioConfigurations)
                    {
                        for (int h = 0; h < g.modelHashes.Length; h++)
                        {
                            if (modelHash == g.modelHashes[h])
                            {
                                speedLimiter = g.speedLimiter;
                                vehicle.SetDriveMaxFlatVelocity(g.driveMaxFlatVelocity / 3.6f);
                                vehicle.SetGearRatios(g.gearRatios);
                                vehicle.HighGear = g.gearRatios.Length - 1;

                                if (showGearConfigNotification)
                                {
                                    StringBuilder stringBuilder = new StringBuilder();
                                    stringBuilder.Append("Loaded gearbox configuration ");
                                    stringBuilder.Append(g.name);
                                    stringBuilder.Append(". \n\nGear ratios:\n\n");

                                    for (int i = 0; i < g.gearRatios.Length; i++)
                                    {
                                        stringBuilder.Append(i == 0 ? "R" : i.ToString());
                                        stringBuilder.Append(": ");
                                        stringBuilder.Append(g.gearRatios[i]);
                                        stringBuilder.Append("\n");
                                    }

                                    stringBuilder.Append("\nTop speed: ");
                                    stringBuilder.Append(g.driveMaxFlatVelocity / g.gearRatios.Last());
                                    stringBuilder.Append(" km/h");

                                    Notification.Show(stringBuilder.ToString());
                                }

                                hasGearboxConfig = true;
                                break;
                            }
                        }

                        if (hasGearboxConfig) break;
                    }

                    if (enableFuelScript)
                    {
                        fuelTankVolume = vehicle.HandlingData.PetrolTankVolume;
                        playedFuelFinishSound = false;
                    }
                    if (!hasGearboxConfig) speedLimiter = 0.0f;

                    baseGearRatios = vehicle.GetGearRatios();
                    topGear = vehicle.HighGear;
                    indicatorState = (int)(vehicle.GetLightStates() >> 8) & 3;
                }

                if (enableEngineControl)
                {
                    if (engineKeyDown && !engineKeyDownLastFrame)
                    {
                        if (vehicle.Speed < 1.0f)
                        {
                            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, !engineState, false, true);
                            engineState = vehicle.IsEngineRunning;
                            if (enableKeyTurnAnimation) playerPed.Task.PlayAnimation("oddjobs@towing", "start_engine", 5.0f, 5.0f, 800, AnimationFlags.None, 0.35f);
                        }
                        if (!engineState)
                        {
                            cruiseControlActive = false;
                        }

                        engineKeyDownLastFrame = true;
                    }
                    else if (!engineKeyDown && engineKeyDownLastFrame)
                    {
                        engineKeyDownLastFrame = false;
                    }
                }

                if (leftIndicatorKeyDown && !leftIndicatorKeyDownLastFrame)
                {
                    indicatorState = (indicatorState == 1) ? 0 : 1;
                    leftIndicatorKeyDownLastFrame = true;
                }
                else if (!leftIndicatorKeyDown && leftIndicatorKeyDownLastFrame)
                {
                    leftIndicatorKeyDownLastFrame = false;
                }

                if (rightIndicatorKeyDown && !rightIndicatorKeyDownLastFrame)
                {
                    indicatorState = (indicatorState == 2) ? 0 : 2;
                    rightIndicatorKeyDownLastFrame = true;
                }
                else if (!rightIndicatorKeyDown && rightIndicatorKeyDownLastFrame)
                {
                    rightIndicatorKeyDownLastFrame = false;
                }

                if (hazardKeyDown && !hazardKeyDownLastFrame)
                {
                    indicatorState = indicatorState == 3 ? 0 : 3;
                    hazardKeyDownLastFrame = true;
                }
                else if (!hazardKeyDown && hazardKeyDownLastFrame)
                {
                    hazardKeyDownLastFrame = false;
                }

                vehicle.IsLeftIndicatorLightOn = indicatorState == 1 || indicatorState == 3;
                vehicle.IsRightIndicatorLightOn = indicatorState == 2 || indicatorState == 3;

                if (raceModeKeyDown && !raceModeKeyDownLastFrame)
                {
                    raceModeActive = !raceModeActive;
                    raceModeKeyDownLastFrame = true;
                }
                else if (!raceModeKeyDown && raceModeKeyDownLastFrame)
                {
                    raceModeKeyDownLastFrame = false;
                }

                bool accelerateKey = Game.IsControlPressed(GTA.Control.VehicleAccelerate);
                bool brakeKey = Game.IsControlPressed(GTA.Control.VehicleBrake);
                bool burnout = accelerateKey && brakeKey;

                float currentRPM = vehicle.CurrentRPM;
                float deltaRPM = (currentRPM - previousRPM) / Time.deltaTime;
                previousRPM = currentRPM;

                float fuelLevel = vehicle.FuelLevel;
                //float deltaFuel = (fuelLevel - fuelLevelLastFrame) / Time.deltaTime;
                //fuelLevelLastFrame = fuelLevel;
                bool accelerateKeyJustPressed = Game.IsControlJustPressed(GTA.Control.VehicleAccelerate);
                bool brakeKeyJustPressed = Game.IsControlJustPressed(GTA.Control.VehicleBrake);
                float torqueMultiplier = (isElectric || burnout) ? 1.0f : MathExt.Clamp(100.0f * (1.0f - currentRPM), 0.0f, 1.0f);

                int wheelCount = vehicle.GetWheelCount();
                int poweredWheelCount = 0;
                int steeredWheelCount = 0;

                driveMaxFlatVelocity = vehicle.GetDriveMaxFlatVelocity();

                velocity = vehicle.Velocity.Length();
                velocityAcceleration = (velocity - velocityLastFrame) / Time.deltaTime;
                velocityLastFrame = velocity;

                Wheel[] wheels = new Wheel[wheelCount];
                float averageDriveWheelAngularVelocity = 0.0f;
                float averageForwardDriveWheelSpeed = 0.0f;
                float averageForwardSteerWheelSpeed = 0.0f;
                float averageWheelSpeed = 0.0f;

                if (!shifting)
                {
                    baseGearRatios = vehicle.GetGearRatios();
                    topGear = vehicle.HighGear;
                }

                for (uint i = 0; i < wheelCount; i++)
                {
                    wheels[i] = new Wheel(vehicle, i);

                    if (wheels[i].isWheelPowered)
                    {
                        poweredWheelCount++;
                        averageDriveWheelAngularVelocity += wheels[i].angularVelocity;
                        averageForwardDriveWheelSpeed += wheels[i].forwardSpeed;
                    }
                    if (wheels[i].isWheelSteered)
                    {
                        steeredWheelCount++;
                        averageForwardSteerWheelSpeed += wheels[i].forwardSpeed;
                    }

                    averageWheelSpeed += wheels[i].forwardSpeed;
                }

                if (poweredWheelCount == 0)
                {
                    averageForwardDriveWheelSpeed = vehicle.WheelSpeed;

                    if (averageForwardDriveWheelSpeed == 0.0f)
                    {
                        averageForwardDriveWheelSpeed = vehicle.Speed;
                    }
                }
                else
                {
                    averageDriveWheelAngularVelocity /= poweredWheelCount;
                    averageForwardDriveWheelSpeed /= poweredWheelCount;
                }

                averageWheelSpeed /= wheelCount;

                if (steeredWheelCount == 0)
                {
                    averageForwardSteerWheelSpeed = vehicle.WheelSpeed;

                    if (averageForwardSteerWheelSpeed == 0.0f)
                    {
                        averageForwardSteerWheelSpeed = vehicle.Speed;
                    }
                }
                else
                {
                    averageForwardSteerWheelSpeed /= steeredWheelCount;
                }
                /*
                odometer += (decimal)averageWheelSpeed * (decimal)Time.deltaTime; 

                float timeToDepletion = deltaFuel < 0.0f ? fuelLevel / (-deltaFuel) : 0.0f;
                float fuelRange = timeToDepletion * averageWheelSpeed * 0.001f;
                averageRange += (fuelRange - averageRange) * 0.01f;
                StringBuilder rangeInfo = new StringBuilder();

                rangeInfo.Append("Range: ");
                rangeInfo.Append(fuelRange.ToString("0"));
                rangeInfo.Append(" km");

                rangeInfo.Append("\nAverage Range: ");
                rangeInfo.Append(averageRange.ToString("0"));
                rangeInfo.Append(" km");

                rangeInfo.Append("\nOdometer: ");
                rangeInfo.Append(((float)odometer * 0.001f).ToString("0.000"));
                rangeInfo.Append(" km");

                GTA.UI.Screen.ShowSubtitle(rangeInfo.ToString());
                */
                forwardSpeed = vehicle.GetForwardSpeed();
                forwardAcceleration = (forwardSpeed - forwardSpeedLastFrame) / Time.deltaTime;
                forwardSpeedLastFrame = forwardSpeed;

                forwardDriveAcceleration = (averageForwardDriveWheelSpeed - forwardDriveWheelSpeedLastFrame) / Time.deltaTime;
                forwardDriveWheelSpeedLastFrame = averageForwardDriveWheelSpeed;

                float displaySpeed = UI.useWheelSpeed ? MathExt.Abs(averageWheelSpeed) : MathExt.Abs(vehicle.Velocity.Length());

                if (accelerateKeyJustPressed)
                {
                    if (accelerateDoubleTapTimer < doubleTapThreshold || burnout || raceModeActive)
                    {
                        accelerateDoubleTap = true;
                    }

                    accelerateDoubleTapTimer = 0.0f;
                }
                else if (!accelerateKey)
                {
                    accelerateDoubleTap = false;
                    accelerateDoubleTapTimer += Time.deltaTime;
                }

                if (brakeKeyJustPressed)
                {
                    if (brakeDoubleTapTimer < doubleTapThreshold || raceModeActive)
                    {
                        brakeDoubleTap = true;
                    }

                    brakeDoubleTapTimer = 0.0f;
                }
                else if (!brakeKey)
                {
                    brakeDoubleTap = false;
                    brakeDoubleTapTimer += Time.deltaTime;
                }

                if (burnout && !burnoutLastFrame)
                {
                    direction = 1;
                    currentGear = 1;
                    vehicle.SetGearRatios(baseGearRatios);
                    brakeKey = false;
                    shifting = false;
                    shiftTimer = 0.0f;
                    shiftWaitTimer = 0.0f;
                    throttleChangeTimer = 0.0f;
                    burnoutLastFrame = true;
                }
                else if (burnoutLastFrame)
                {
                    burnoutLastFrame = false;
                }

                if (!burnout && directionSwitchKeyDown && (forwardSpeed < 1.0f || direction == -1))
                {
                    Game.DisableControlThisFrame(GTA.Control.VehicleAccelerate);
                    Game.DisableControlThisFrame(GTA.Control.VehicleBrake);

                    if (accelerateKey && !brakeKey)
                    {
                        direction = 1;
                        currentGear = 1;
                        throttleChangeTimer = 0.0f;

                        if (enableAdvancedGearbox)
                        {
                            vehicle.SetGearRatios(baseGearRatios);
                            shifting = false;
                            shiftTimer = 0.0f;
                            shiftWaitTimer = 0.0f;
                        }
                    }
                    else if (!accelerateKey && brakeKey)
                    {
                        direction = -1;
                        currentGear = 0;
                        throttleChangeTimer = 0.0f;

                        if (enableAdvancedGearbox)
                        {
                            vehicle.SetGearRatios(baseGearRatios);
                            shifting = false;
                            shiftTimer = 0.0f;
                            shiftWaitTimer = 0.0f;
                        }
                    }
                }

                if (burnout)
                {
                    vehicle.ThrottlePower = 1.0f;
                    vehicle.Clutch = 1.0f;
                    cruiseControlActive = false;
                }
                else if (direction == 1)
                {
                    if (cruiseKeyDown && !cruiseKeyDownLastFrame)
                    {
                        if (cruiseControlActive)
                        {
                            cruiseControlActive = false;
                            preventSpeedChange = false;
                        }
                        else if (forwardSpeed >= cruiseMinSpeed)
                        {
                            preventSpeedChange = true;
                            cruiseSpeed = UI.isLoaded ? UI.displaySpeed : displaySpeed;
                            cruiseControlActive = true;
                            cruiseThrottle = 0.0f;
                        }

                        cruiseKeyDownLastFrame = true;
                    }
                    else if (!cruiseKeyDown && cruiseKeyDownLastFrame)
                    {
                        cruiseKeyDownLastFrame = false;
                    }

                    if (cruiseResumeKeyDown && !cruiseResumeKeyDownLastFrame)
                    {
                        if (!cruiseControlActive && cruiseSpeed > cruiseMinSpeed)
                        {
                            preventSpeedChange = true;
                            cruiseControlActive = true;
                            cruiseThrottle = 0.0f;
                        }
                    }
                    else if (!cruiseResumeKeyDown && cruiseResumeKeyDownLastFrame)
                    {
                        cruiseResumeKeyDownLastFrame = false;
                    }

                    if (accelerateKey && !accelerateDoubleTap)
                    {
                        if (!lightThrottleLastFrame)
                        {
                            throttle = 0.0f;
                            lightThrottleLastFrame = true;
                            throttleChangeTimer = throttleChangeDelay;
                        }

                        if (throttleChangeTimer >= throttleChangeDelay || forwardAcceleration < 0.0f || forwardSpeed < 2.5f)
                        {
                            if (forwardAcceleration < minForwardAcceleration)
                            {
                                throttle = MathExt.Clamp(throttle + 0.25f * Time.deltaTime * (targetForwardAcceleration - forwardAcceleration), 0.0f, maxThrottle);
                            }
                            else if (forwardAcceleration > maxForwardAcceleration)
                            {
                                throttle = MathExt.Clamp(throttle + 0.25f * Time.deltaTime * (targetForwardAcceleration - forwardAcceleration), 0.0f, maxThrottle);
                            }
                        }
                        else
                        {
                            throttleChangeTimer += Time.deltaTime;
                        }
                    }
                    else if (accelerateKey && accelerateDoubleTap)
                    {
                        if (lightThrottleLastFrame)
                        {
                            lightThrottleLastFrame = false;
                        }

                        throttle = 1.0f;
                    }
                    else
                    {
                        if (lightThrottleLastFrame)
                        {
                            lightThrottleLastFrame = false;
                        }

                        throttle = 0.0f;
                    }

                    if (brakeKey && !brakeDoubleTap)
                    {
                        if (!lightBrakeLastFrame)
                        {
                            brake = 0.0f;
                            lightBrakeLastFrame = true;
                        }

                        brake = MathExt.Clamp(brake - 10.0f * Time.deltaTime * (-defaultBrakeDeceleration - forwardAcceleration), 0.0f, 1.0f);

                        if (brake > 0.0f) vehicle.AreBrakeLightsOn = true;
                    }
                    else if (brakeKey && brakeDoubleTap)
                    {
                        if (lightBrakeLastFrame)
                        {
                            lightBrakeLastFrame = false;
                        }

                        brake = 1.0f;
                    }
                    else
                    {
                        if (lightBrakeLastFrame)
                        {
                            lightBrakeLastFrame = false;
                        }

                        brake = 0.0f;
                    }

                    if (brakeDoubleTapTimer <= doubleTapThreshold)
                    {
                        vehicle.AreBrakeLightsOn = true;
                    }

                    if (cruiseControlActive && !accelerateKey && !brakeKey)
                    {
                        bool autoFollow = false;

                        if (enableAutoFollow)
                        {
                            Vector3 A = vehicle.Position;
                            float theta = vehicle.Heading - 270.0f + vehicle.SteeringAngle;
                            float mod90 = theta % 90.0f;
                            if (mod90 < 1.0f) theta += 1.0f;
                            if (mod90 > 89.0f) theta -= 1.0f;
                            float m = MathExt.Tan(theta);
                            float b = A.Y - A.X * m;
                            float mp = -1.0f / m;

                            if (!preventSpeedChange)
                            {
                                if (cruiseFasterKeyDown)
                                {
                                    cruiseSpeed += UI.cruiseSpeedIncrement * Time.deltaTime;
                                }
                                else if (cruiseSlowerKeyDown)
                                {
                                    cruiseSpeed = Math.Max(cruiseSpeed - UI.cruiseSpeedIncrement * Time.deltaTime, 0.0f);
                                }
                            }
                            else if (!cruiseFasterKeyDown && !cruiseSlowerKeyDown)
                            {
                                preventSpeedChange = false;
                            }

                            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(vehicle.FrontPosition + vehicle.ForwardVector * 60.0f, 60.0f).OrderBy((Vehicle v) => v.Position.DistanceTo(vehicle.FrontPosition)).ToArray();

                            for (int i = 0; i < nearbyVehicles.Length; i++)
                            {
                                if (nearbyVehicles[i] != vehicle && (nearbyVehicles[i].Driver != null))
                                {
                                    float heading = nearbyVehicles[i].Heading - vehicle.Heading;
                                    if (heading < -180.0f) heading += 360.0f;
                                    if (heading > 180.0f) heading -= 360.0f;

                                    if (heading > -45.0f && heading < 45.0f)
                                    {
                                        Vector3 B = nearbyVehicles[i].Position;
                                        float bp = B.Y - mp * B.X;
                                        float x = (bp - b) / (m - mp);
                                        float y = m * x + b;
                                        float d = Vector3.Distance2D(new Vector3(x, y, 0.0f), nearbyVehicles[i].Position);

                                        if (d < 3.0f)
                                        {
                                            autoFollow = true;

                                            float forwardSpeed2 = nearbyVehicles[i].GetForwardSpeed();
                                            float distance = vehicle.FrontPosition.DistanceTo(nearbyVehicles[i].RearPosition);

                                            if (forwardSpeed2 < 0.1f && forwardSpeed2 > -0.1f && distance < 1.5f)
                                            {
                                                brake = 0.25f;
                                                vehicle.AreBrakeLightsOn = true;
                                            }
                                            else
                                            {
                                                float relativeSpeed = forwardSpeed - forwardSpeed2;

                                                float targetDistance = Math.Max(forwardSpeed * cruiseFollowingDistance, 0.0f);
                                                float predictedDistance = distance - relativeSpeed * 1.2f - targetDistance;

                                                brake = MathExt.Clamp(-predictedDistance / targetDistance, 0.0f, 1.0f);

                                                if (brake > 0.0f)
                                                {
                                                    vehicle.AreBrakeLightsOn = true;
                                                    cruiseThrottle = 0.0f;
                                                }
                                                else
                                                {
                                                    float targetSpeed = Math.Min(forwardSpeed + Math.Min(5.0f * (predictedDistance - 1.0f), 15.0f), cruiseSpeed);
                                                    float targetAcceleration = MathExt.Clamp((targetSpeed - displaySpeed) * cruiseTgtAccFactor, -cruiseMaxAcceleration, cruiseMaxAcceleration);
                                                    cruiseThrottle = MathExt.Clamp(cruiseThrottle + (targetAcceleration - forwardAcceleration) * cruiseAccCorrFactor * Time.deltaTime, 0.0f, 1.0f);
                                                }
                                            }

                                            throttle = MathExt.Clamp(throttle + cruiseThrottle, 0.0f, 1.0f);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (!autoFollow)
                        {
                            float targetAcceleration = MathExt.Clamp((cruiseSpeed - displaySpeed) * cruiseTgtAccFactor, -cruiseMaxAcceleration, cruiseMaxAcceleration);
                            cruiseThrottle = MathExt.Clamp(cruiseThrottle + (targetAcceleration - forwardAcceleration) * cruiseAccCorrFactor * Time.deltaTime, 0.0f, 1.0f);
                            throttle = MathExt.Clamp(throttle + cruiseThrottle, 0.0f, 1.0f);
                        }
                    }

                    if (!isElectric && enableAdvancedGearbox)
                    {
                        if (currentGear < 1)
                        {
                            currentGear = 1;
                        }
                        if (currentGear > topGear)
                        {
                            currentGear = topGear;
                        }

                        if (useManualGearbox)
                        {
                            if (downShiftKeyDown && !downShiftKeyDownLastFrame)
                            {
                                if (currentGear > 1)
                                {
                                    lastGear = currentGear;
                                    currentGear--;
                                    shifting = true;
                                }

                                downShiftKeyDownLastFrame = true;
                            }
                            else if (!downShiftKeyDown)
                            {
                                downShiftKeyDownLastFrame = false;
                            }

                            if (upShiftKeyDown && !upShiftKeyDownLastFrame)
                            {
                                if (currentGear < topGear)
                                {
                                    lastGear = currentGear;
                                    currentGear++;
                                    shifting = true;
                                }

                                upShiftKeyDownLastFrame = true;
                            }
                            else if (!upShiftKeyDown)
                            {
                                upShiftKeyDownLastFrame = false;
                            }
                        }
                        else
                        {
                            int targetGear = 1;
                            float trThrottle = MathExt.Clamp((throttle - 0.2f) / 0.6f, 0.0f, 1.0f);

                            if (brakeKey)
                            {
                                targetRPM += MathExt.Clamp(0.4f - targetRPM, -targetRPMFallRate * (1.0f + brake) * Time.deltaTime, 0.0f);
                                decelUpShiftTimer = 5.0f;
                                decelMode = false;
                            }
                            else if (throttle == 0.0f)
                            {
                                if (targetRPM >= 0.6f)
                                {
                                    targetRPM = 0.6f;

                                    if (decelUpShiftTimer < 5.0f)
                                    {
                                        decelMode = true;
                                        decelUpShiftTimer += Time.deltaTime;
                                    }
                                    else
                                    {
                                        decelMode = false;
                                    }
                                }
                                else
                                {
                                    targetRPM = 0.4f;
                                    decelUpShiftTimer = 5.0f;
                                    decelMode = false;
                                }
                            }
                            else if (trThrottle < 1.0f)
                            {
                                targetRPM += MathExt.Clamp(0.4f + trThrottle * trThrottle * 0.6f - targetRPM, -targetRPMFallRate * Time.deltaTime * (1.0f - trThrottle), targetRPMRiseRate * Time.deltaTime * trThrottle);
                                decelUpShiftTimer = 5.0f;
                                decelMode = false;
                            }
                            else
                            {
                                targetRPM = 0.99f;
                                decelUpShiftTimer = 0.0f;
                                decelMode = false;
                            }

                            if (averageForwardDriveWheelSpeed > 0.1f)
                            {
                                float targetGearRatio = targetRPM * driveMaxFlatVelocity / averageForwardDriveWheelSpeed;

                                for (; targetGear < topGear; targetGear++)
                                {
                                    if (baseGearRatios[targetGear] <= targetGearRatio)
                                    {
                                        if (targetGear < currentGear)
                                        {
                                            float gearRatio = baseGearRatios[targetGear];
                                            float gearRPM = averageForwardDriveWheelSpeed / (driveMaxFlatVelocity / gearRatio);
                                            float gearRatioRatio = baseGearRatios[currentGear] / gearRatio;
                                            float gearTimeToRedline = deltaRPM > 0.0f ? (1.0f - gearRPM) / (deltaRPM * gearRatioRatio) : -1.0f;

                                            if (gearRPM < 0.8f && (gearTimeToRedline >= 1.5f || gearTimeToRedline == -1.0f))
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                            //GTA.UI.Screen.ShowSubtitle(currentRPM.ToString("0.00") + (deltaRPM >= 0.0f ? " + " : " - ") + deltaRPM.ToString("0.00000").TrimStart('-'));

                            if (!shifting && currentGear != targetGear)
                            {
                                shiftWaitTimer += Time.deltaTime;

                                if
                                (
                                    (shiftWaitTimer >= shiftDelay ||
                                    currentRPM < 0.3f ||
                                    ((currentRPM >= 0.9f && deltaRPM < 0.01f) || currentRPM > 0.99f) ||
                                    (targetGear < currentGear && throttle >= 0.9f)) &&
                                    (!decelMode || targetGear < currentGear)
                                )
                                {
                                    lastGear = currentGear;
                                    currentGear = targetGear;
                                    shiftWaitTimer = 0.0f;
                                    shifting = true;
                                    shiftTimer = 0.0f;
                                    throttleChangeTimer = 0.0f;
                                }
                            }
                            else if (shiftWaitTimer > 0.0f)
                            {
                                shiftWaitTimer = 0.0f;
                            }
                        }

                        if (shifting)
                        {
                            float gearRatioI = 0.5f * ((float)Math.Sin(MathExt.pi * (MathExt.Clamp(shiftTimer / (lastGear < currentGear ? upShiftDuration : downShiftDuration), 0.0f, 1.0f) - 0.5f)) + 1.0f);
                            shiftTimer += Time.deltaTime;

                            if ((shiftTimer > upShiftDuration && lastGear < currentGear) || (shiftTimer > downShiftDuration && lastGear > currentGear))
                            {
                                vehicle.SetGearRatios(baseGearRatios);
                                shifting = false;
                                shiftTimer = 0.0f;
                            }
                            else
                            {
                                tempGearRatios = baseGearRatios.ToArray();
                                tempGearRatios[currentGear] = baseGearRatios[lastGear] + (baseGearRatios[currentGear] - baseGearRatios[lastGear]) * gearRatioI;
                                vehicle.SetGearRatios(tempGearRatios);
                            }
                        }

                        vehicle.Clutch = 1.0f;
                    }

                    if (speedLimiter > 0.0f && averageWheelSpeed > speedLimiter) throttle = MathExt.Clamp(throttle - 4.0f * (averageForwardDriveWheelSpeed - speedLimiter), 0.0f, 1.0f);

                    if (enableTractionControl)
                    {
                        float accelerationThreshold = 1.3f * velocityAcceleration;
                        float overAcceleration = forwardDriveAcceleration - accelerationThreshold;
                        float overspeedFactor = (averageForwardSteerWheelSpeed > 0.0f) ? (averageForwardDriveWheelSpeed / averageForwardSteerWheelSpeed - 1.0f) : averageForwardDriveWheelSpeed;

                        bool tcsAcceleration = throttle > 0.0f && accelerationThreshold > 0.0f && overAcceleration > 0.0f;
                        bool tcsOverspeed = throttle > 0.0f && overspeedFactor > 0.5f;

                        if (tcsAcceleration || tcsOverspeed)
                        {
                            tcsTimer += Time.deltaTime;

                            if (tcsOverspeed && (averageForwardDriveWheelSpeed < -2.0f || averageForwardDriveWheelSpeed > 2.0f))
                            {
                                tcsFactor = Math.Max(tcsFactor - 50.0f * Time.deltaTime * overspeedFactor, 0.0f);
                            }
                            else if (tcsTimer >= tcsDelay)
                            {
                                tcsFactor = Math.Max(tcsFactor - 25.0f * Time.deltaTime * overAcceleration / velocityAcceleration, 0.0f);
                            }
                        }
                        else
                        {
                            tcsTimer = 0.0f;
                            tcsFactor = Math.Min(1.0f, tcsFactor + Time.deltaTime * 2.0f);
                        }

                        vehicle.ThrottlePower = throttle * tcsFactor;
                    }
                    else
                    {
                        vehicle.ThrottlePower = throttle;
                    }
                }
                else if (direction == -1)
                {
                    if (brakeKey && !brakeDoubleTap)
                    {
                        throttle = MathExt.Clamp(throttle + Time.deltaTime * 0.5f, 0.0f, 0.3f);
                    }
                    else if (brakeKey && brakeDoubleTap)
                    {
                        throttle = MathExt.Clamp(throttle + Time.deltaTime * 5.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        throttle = MathExt.Clamp(throttle - Time.deltaTime * 15.0f, 0.0f, 1.0f);
                    }

                    if (accelerateKey && !accelerateDoubleTap)
                    {
                        brake = MathExt.Clamp(brake + Time.deltaTime * 0.5f, 0.0f, 0.3f);
                    }
                    else if (accelerateKey && accelerateDoubleTap)
                    {
                        brake = MathExt.Clamp(brake + Time.deltaTime * 5.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        brake = MathExt.Clamp(brake - Time.deltaTime * 15.0f, 0.0f, 1.0f);
                    }

                    if (!isElectric && enableAdvancedGearbox)
                    {
                        vehicle.Clutch = 1.0f;

                        if (currentGear != 0)
                        {
                            currentGear = 0;
                        }
                    }

                    if (enableTractionControl)
                    {
                        float accelerationThreshold = -1.75f * velocityAcceleration;
                        float overAcceleration = accelerationThreshold - forwardDriveAcceleration;
                        float overspeedFactor = (averageForwardSteerWheelSpeed < 0.0f) ? (averageForwardDriveWheelSpeed / averageForwardSteerWheelSpeed - 1.0f) : averageForwardDriveWheelSpeed;

                        bool tcsAcceleration = throttle > 0.0f && accelerationThreshold < 0.0f && overAcceleration > 0.0f;
                        bool tcsOverspeed = throttle > 0.0f && overspeedFactor > 0.3f;

                        if (tcsAcceleration || tcsOverspeed)
                        {
                            tcsTimer += Time.deltaTime;

                            if (tcsOverspeed && (averageForwardDriveWheelSpeed < -0.1f || averageForwardDriveWheelSpeed > 0.1f))
                            {
                                tcsFactor = 0.0f;
                            }
                            else if (tcsTimer >= tcsDelay)
                            {
                                tcsFactor = Math.Max(tcsFactor - 10.0f * Time.deltaTime * overAcceleration / velocityAcceleration, 0.0f);
                            }
                        }
                        else
                        {
                            tcsTimer = 0.0f;
                            tcsFactor = Math.Min(1.0f, tcsFactor + Time.deltaTime * 5.0f);
                        }

                        vehicle.ThrottlePower = Math.Min(-throttle * tcsFactor, -0.1f);
                    }
                    else
                    {
                        vehicle.ThrottlePower = Math.Min(-throttle, -0.1f);
                    }

                    torqueMultiplier *= MathExt.Clamp(throttle / 0.1f, 0.0f, 1.0f); //To ensure reverse lights remain on cos gta devs don't know how cars work xdddd
                }
                else
                {
                    throttle = 0.0f;

                    if (brakeKey && !brakeDoubleTap)
                    {
                        throttle = MathExt.Clamp(throttle + Time.deltaTime * 0.5f, 0.0f, 0.3f);
                    }
                    else if (brakeKey && brakeDoubleTap)
                    {
                        throttle = MathExt.Clamp(throttle + Time.deltaTime * 5.0f, 0.0f, 1.0f);
                    }
                    else
                    {
                        throttle = MathExt.Clamp(throttle - Time.deltaTime * 15.0f, 0.0f, 1.0f);
                    }

                    if (enableAdvancedGearbox)
                    {
                        if (currentGear != 0)
                        {
                            currentGear = 0;
                        }
                    }

                    torqueMultiplier = 0.0f;
                }

                if (enableAdvancedGearbox && vehicle.CurrentGear != currentGear)
                {
                    vehicle.NextGear = currentGear;
                    vehicle.CurrentGear = currentGear;
                }

                vehicle.EngineTorqueMultiplier = torqueMultiplier;
                vehicle.BrakePower = burnout ? 1.0f : brake;

                if (enableSmoothSteering)
                {
                    if (Game.IsControlPressed(GTA.Control.VehicleMouseControlOverride))
                    {
                        if (steeringPatchers[0].Status > 1)
                            foreach (Memory.MemoryPatcher m in steeringPatchers)
                            {
                                m.Revert();
                            }

                        steering = vehicle.SteeringScale;
                        steeringInput = 0.0f;
                    }
                    else
                    {
                        if (steeringPatchers[0].Status < 2)
                            foreach (Memory.MemoryPatcher m in steeringPatchers)
                            {
                                m.Apply();
                            }

                        bool left = Game.IsControlPressed(GTA.Control.VehicleMoveLeftOnly);
                        bool right = Game.IsControlPressed(GTA.Control.VehicleMoveRightOnly);
                        float speedCompensation = 1.0f + steeringSpeedCompensation * MathExt.Abs(averageForwardSteerWheelSpeed);

                        if (left && !right)
                        {
                            if (steeringInput < 0.0f) steeringInput = -steeringInput;
                            steeringInput = Math.Min(1.0f, steeringInput + speedCompensation * Time.deltaTime * steeringInputRise);
                        }
                        else if (!left && right)
                        {
                            if (steeringInput > 0.0f) steeringInput = -steeringInput;
                            steeringInput = Math.Max(-1.0f, steeringInput - speedCompensation * Time.deltaTime * steeringInputRise);
                        }
                        else
                        {
                            float inc = speedCompensation * Time.deltaTime * steeringInputFall;

                            if (steeringInput > inc)
                            {
                                steeringInput = Math.Max(0.0f, steeringInput - inc);
                            }
                            else if (steeringInput < inc)
                            {
                                steeringInput = Math.Min(0.0f, steeringInput + inc);
                            }
                            else
                            {
                                steeringInput = 0.0f;
                            }
                        }

                        float absSteeringInput = MathExt.Abs(steeringInput);
                        if (absSteeringInput < 0.01f) steeringInput = 0.0f;
                        steering = MathExt.Clamp(steering + Time.deltaTime * steeringSensitivity * steeringInput * speedCompensation, -1.0f, 1.0f);

                        if (absSteeringInput < 0.95f)
                        {
                            float steeringCenterMultiplier = (1.0f - absSteeringInput) * MathExt.Clamp(1.0f - (float)Math.Pow(1.0f / (steeringCentering * averageForwardSteerWheelSpeed * averageForwardSteerWheelSpeed + 1.0f), Time.deltaTime), 0.0f, 1.0f);
                            steering += (0.0f - steering) * steeringCenterMultiplier;
                        }

                        vehicle.SteeringScale += (steering - vehicle.SteeringScale) * MathExt.Clamp(1.0f - (float)Math.Pow(steeringInputSmoothing, Time.deltaTime), 0.0f, 1.0f);
                    }
                }
                else steering = vehicle.SteeringScale;

                if ((indicatorState == 1 && steering >= indicatorCutoffArmThreshold && !armIndicatorCutoff) || (indicatorState == 2 && -steering >= indicatorCutoffArmThreshold))
                {
                    armIndicatorCutoff = true;
                }
                else if (armIndicatorCutoff && ((indicatorState == 1 && steering <= indicatorCutoffThreshold) || (indicatorState == 2 && -steering <= indicatorCutoffThreshold)))
                {
                    indicatorState = 0;
                    armIndicatorCutoff = false;
                }
                else if (armIndicatorCutoff && (indicatorState == 0 || indicatorState == 3))
                {
                    armIndicatorCutoff = false;
                }

                if (enableFuelScript && engineState)
                {
                    float fuelThrottle = currentRPM > 0.2f ? vehicle.ThrottlePower : 0.1f;
                    float fuelConsumption = fuelConsumptionModifier * fuelThrottle * currentRPM * torqueMultiplier * MathExt.Clamp(1.7f - currentRPM, 0.0f, 1.0f);

                    if (fuelConsumption > 0.0f)
                    {
                        for (int i = 0; i < 100; i++) //To prevent the fuel consumption rate from being too small to transcend 32-bit float precision
                        {
                            vehicle.FuelLevel = MathExt.Clamp(vehicle.FuelLevel - fuelTankVolume * Time.deltaTime * fuelConsumption, 0.0f, fuelTankVolume);

                            if (vehicle.FuelLevel != fuelLevel)
                            {
                                fuelLevel = vehicle.FuelLevel;
                                break;
                            }

                            fuelConsumption *= 1.05f;
                        }
                    }
                }

                if (UI.isLoaded)
                {
                    float displayRPM = MathExt.Clamp(currentRPM / 0.99f, 0.2f, 1.0f);
                    bool headlights = vehicle.AreLightsOn;
                    bool highbeams = vehicle.AreHighBeamsOn;
                    UI.Update(engineState, displaySpeed, cruiseControlActive, raceModeActive, cruiseSpeed, displayRPM, enableFuelScript ? (vehicle.FuelLevel / fuelTankVolume) : 1.0f, vehicle.CurrentGear, indicatorState, vehicle.GetIndicatorFlash(), headlights, highbeams);
                }

                inVehicleLastFrame = true;
            }
            else
            {
                if (inVehicleLastFrame)
                {
                    previousVehicle.SetGearRatios(baseGearRatios);
                    armIndicatorCutoff = false;
                    baseGearRatios = null;
                    currentGear = 1;
                    shifting = false;
                    steering = 0.0f;
                    direction = 1;

                    if (enableEngineControl)
                    {
                        Function.Call(Hash.SET_VEHICLE_ENGINE_ON, previousVehicle, engineState, true, true);
                    }

                    foreach (Memory.MemoryPatcher m in vehiclePatchers)
                    {
                        m.Revert();
                    }

                    foreach (Memory.MemoryPatcher m in steeringPatchers)
                    {
                        m.Revert();
                    }

                    engineState = false;
                    indicatorState = 0;
                    raceModeActive = false;
                    inVehicleLastFrame = false;
                    jerryCanDrainTimer = 0.0f;
                    UI.Reset();
                }

                if (enableFuelScript && previousVehicle != null && previousVehicle.Exists())
                {
                    Weapon current = playerPed.Weapons.Current;
                    bool fuelPump = World.GetNearbyProps(playerPed.Position, 3.0f, fuelPumpModels).Length > 0;
                    bool jerryCan = !fuelPump && (playerPed.Weapons.Current.Hash == WeaponHash.PetrolCan);

                    if ((fuelPump || jerryCan) && playerPed.Position.DistanceTo(previousVehicle.Position) < 5.0f)
                    {
                        fuelTankVolume = previousVehicle.HandlingData.PetrolTankVolume;
                        float fuelLevel = previousVehicle.FuelLevel / fuelTankVolume;

                        if (previousVehicle.IsEngineRunning)
                        {
                            GTA.UI.Screen.ShowSubtitle("~r~Cannot refuel - engine running!", 50);
                        }
                        else if (jerryCan && current.AmmoInClip <= 0)
                        {
                            GTA.UI.Screen.ShowSubtitle("~r~Cannot refuel - jerry can empty!", 50);
                        }
                        else if (fuelLevel < 1.0f)
                        {
                            Game.DisableControlThisFrame(GTA.Control.Pickup);
                            GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_PICKUP~ to refuel.");

                            if (Game.IsControlPressed(GTA.Control.Pickup))
                            {
                                if (jerryCan)
                                {
                                    jerryCanDrainTimer += Time.deltaTime * jerryCanDrainRate * fuelPumpRate;
                                    if (jerryCanDrainTimer > 1)
                                    {
                                        current.AmmoInClip -= (int)(jerryCanDrainTimer % current.MaxAmmoInClip + 0.5f);
                                        jerryCanDrainTimer = 0.0f;
                                    }
                                }

                                previousVehicle.FuelLevel = MathExt.Clamp(previousVehicle.FuelLevel + Time.deltaTime * fuelPumpRate, 0.0f, fuelTankVolume);
                                playedFuelFinishSound = false;
                            }

                            GTA.UI.Screen.ShowSubtitle("~o~Fuel Level: " + fuelLevel.ToString("0.00%") + " (" + previousVehicle.FuelLevel.ToString("0.0") + "/" + fuelTankVolume.ToString("0.0") + " L)", 50);
                        }
                        else
                        {
                            GTA.UI.Screen.ShowSubtitle("~g~Fuel Level: " + fuelLevel.ToString("0.00%") + " (" + previousVehicle.FuelLevel.ToString("0.0") + "/" + fuelTankVolume.ToString("0.0") + " L)", 50);

                            if (!playedFuelFinishSound)
                            {
                                Audio.PlaySoundFrontend("Hack_Success", "DLC_HEIST_BIOLAB_PREP_HACKING_SOUNDS");
                                playedFuelFinishSound = true;
                            }
                        }
                    }
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (enableEngineControl && keyEventArgs.KeyCode == engineKey && keyEventArgs.Modifiers == engineKeyModifier)
            {
                engineKeyDown = true;
            }
            if (keyEventArgs.KeyCode == cruiseKey && keyEventArgs.Modifiers == cruiseKeyModifier)
            {
                cruiseKeyDown = true;
            }
            if (keyEventArgs.KeyCode == cruiseResumeKey && keyEventArgs.Modifiers == cruiseResumeKeyModifier)
            {
                cruiseResumeKeyDown = true;
            }
            if (keyEventArgs.KeyCode == cruiseFasterKey && keyEventArgs.Modifiers == cruiseFasterKeyModifier)
            {
                cruiseFasterKeyDown = true;
            }
            if (keyEventArgs.KeyCode == cruiseSlowerKey && keyEventArgs.Modifiers == cruiseSlowerKeyModifier)
            {
                cruiseSlowerKeyDown = true;
            }
            if (keyEventArgs.KeyCode == leftIndicatorKey && keyEventArgs.Modifiers == leftIndicatorKeyModifier)
            {
                leftIndicatorKeyDown = true;
            }
            if (keyEventArgs.KeyCode == rightIndicatorKey && keyEventArgs.Modifiers == rightIndicatorKeyModifier)
            {
                rightIndicatorKeyDown = true;
            }
            if (keyEventArgs.KeyCode == hazardKey && keyEventArgs.Modifiers == hazardKeyModifier)
            {
                hazardKeyDown = true;
            }
            if (keyEventArgs.KeyCode == raceModeKey && (raceModeKeyModifier == Keys.None || keyEventArgs.Modifiers == raceModeKeyModifier))
            {
                raceModeKeyDown = true;
            }
            if (keyEventArgs.KeyCode == directionSwitchKey)
            {
                directionSwitchKeyDown = true;
            }
            if (keyEventArgs.KeyCode == downShiftKey && (downShiftKeyModifier == Keys.None || keyEventArgs.Modifiers == downShiftKeyModifier))
            {
                downShiftKeyDown = true;
            }
            if (keyEventArgs.KeyCode == upShiftKey && (upShiftKeyModifier == Keys.None || keyEventArgs.Modifiers == upShiftKeyModifier))
            {
                upShiftKeyDown = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (enableEngineControl && keyEventArgs.KeyCode == engineKey)
            {
                engineKeyDown = false;
            }
            if (keyEventArgs.KeyCode == cruiseKey)
            {
                cruiseKeyDown = false;
            }
            if (keyEventArgs.KeyCode == cruiseResumeKey)
            {
                cruiseResumeKeyDown = false;
            }
            if (keyEventArgs.KeyCode == cruiseFasterKey)
            {
                cruiseFasterKeyDown = false;
            }
            if (keyEventArgs.KeyCode == cruiseSlowerKey)
            {
                cruiseSlowerKeyDown = false;
            }
            if (keyEventArgs.KeyCode == leftIndicatorKey)
            {
                leftIndicatorKeyDown = false;
            }
            if (keyEventArgs.KeyCode == rightIndicatorKey)
            {
                rightIndicatorKeyDown = false;
            }
            if (keyEventArgs.KeyCode == hazardKey)
            {
                hazardKeyDown = false;
            }
            if (keyEventArgs.KeyCode == raceModeKey)
            {
                raceModeKeyDown = false;
            }
            if (keyEventArgs.KeyCode == directionSwitchKey)
            {
                directionSwitchKeyDown = false;
            }
            if (keyEventArgs.KeyCode == downShiftKey)
            {
                downShiftKeyDown = false;
            }
            if (keyEventArgs.KeyCode == upShiftKey)
            {
                upShiftKeyDown = false;
            }
        }

        private void OnAbort(object sender, EventArgs eventArgs)
        {
            foreach (Memory.MemoryPatcher m in runtimePatchers)
            {
                m.Revert();
            }

            foreach (Memory.MemoryPatcher m in steeringPatchers)
            {
                m.Revert();
            }

            foreach (Memory.MemoryPatcher m in vehiclePatchers)
            {
                m.Revert();
            }
        }
    }
}
