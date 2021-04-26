using System;
using System.Drawing;
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
    public class AIGearboxManager : Script
    {
        private int index = 0;
        public static int maxIndex = 20;

        public AIGearboxManager()
        {
            Tick += AIGearboxTick;
        }

        public void AIGearboxTick(object sender, EventArgs eventArgs)
        {
            if (Main.enableAdvancedGearbox)
            {
                Yield();
                Vehicle[] vehicles = World.GetAllVehicles();

                if (maxIndex == 0 || index >= vehicles.Length)
                {
                    index = 0;
                }

                for (int start = index; index < vehicles.Length; index++)
                {
                    if (maxIndex > 0 && index - start >= maxIndex)
                    {
                        break;
                    }

                    Vehicle vehicle = vehicles[index];

                    if (vehicle != null && vehicle.Exists() && vehicle != Game.Player.Character.CurrentVehicle)
                    {
                        List<float> gearRatios = vehicle.GetGearRatios();

                        int wheelCount = vehicle.GetWheelCount();
                        Wheel[] wheels = new Wheel[wheelCount];
                        int poweredWheelCount = 0;

                        float averageForwardDriveWheelSpeed = 0.0f;

                        for (uint j = 0; j < wheelCount; j++)
                        {
                            wheels[j] = new Wheel(vehicle, j);

                            if (wheels[j].isWheelPowered)
                            {
                                poweredWheelCount++;
                                averageForwardDriveWheelSpeed += wheels[j].forwardSpeed;
                            }
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
                            averageForwardDriveWheelSpeed /= poweredWheelCount;
                        }

                        int targetGear = 1;
                        int currentGear = vehicle.CurrentGear;
                        int topGear = vehicle.HighGear;

                        if (averageForwardDriveWheelSpeed > 0.1f)
                        {
                            float throttle = vehicle.ThrottlePower;
                            float driveMaxFlatVelocity = vehicle.GetDriveMaxFlatVelocity();
                            float targetGearRatio = (0.25f + throttle * throttle * 0.70f) * driveMaxFlatVelocity / averageForwardDriveWheelSpeed;

                            for (; targetGear < topGear; targetGear++)
                            {
                                if (gearRatios[targetGear] <= targetGearRatio)
                                {
                                    if (targetGear < currentGear)
                                    {
                                        float gearRPM = averageForwardDriveWheelSpeed / (driveMaxFlatVelocity / gearRatios[targetGear]);

                                        if (gearRPM < 0.8f)
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

                        if (targetGear != vehicle.CurrentGear)
                        {
                            vehicle.NextGear = targetGear;
                            vehicle.CurrentGear = targetGear;
                        }

                        vehicle.Clutch = 1.0f;
                    }
                }
            }
        }
    }
}
