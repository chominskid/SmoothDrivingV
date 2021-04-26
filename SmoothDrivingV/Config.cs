using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothDrivingV
{
    public static class Config
    {
        public static bool ReadBool(ICollection<string> lines, string name, bool fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2)
                    {
                        try
                        {
                            bool output = bool.Parse(sections[1]);
                            Logger.WriteToLog("Config bool '" + name + "' parsed successfully (" + output.ToString() + ").");
                            return output;
                        }
                        catch
                        {

                        }
                    }

                    break;
                }
            }

            Logger.WriteToLog("Config bool '" + name + "' failed to parse - reverting to default (" + fallback.ToString() + ").");
            return fallback;
        }

        public static int ReadInt32(ICollection<string> lines, string name, int fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2)
                    {
                        try
                        {
                            int output = int.Parse(sections[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                            Logger.WriteToLog("Config Int32 '" + name + "' parsed successfully (" + output.ToString() + ").");
                            return output;
                        }
                        catch
                        {

                        }
                    }

                    break;
                }
            }

            Logger.WriteToLog("Config Int32 '" + name + "' failed to parse - reverting to default (" + fallback.ToString() + ").");
            return fallback;
        }

        public static float ReadFloat(ICollection<string> lines, string name, float fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2)
                    {
                        try
                        {
                            float output = float.Parse(sections[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                            Logger.WriteToLog("Config float '" + name + "' parsed successfully (" + output.ToString() + ").");
                            return output;
                        }
                        catch
                        {

                        }
                    }

                    break;
                }
            }

            Logger.WriteToLog("Config float '" + name + "' failed to parse - reverting to default (" + fallback.ToString() + ").");
            return fallback;
        }

        public static string ReadString(ICollection<string> lines, string name, string fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2)
                    {
                        Logger.WriteToLog("Config string '" + name + "' retrieved successfully (" + sections[1] + ").");
                        return sections[1];
                    }

                    break;
                }
            }

            Logger.WriteToLog("Config string '" + name + "' failed to read - reverting to default (" + fallback + ").");
            return fallback;
        }

        public static string[] ReadStrings(ICollection<string> lines, string name, string[] delimiters, string[] fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2)
                    {
                        Logger.WriteToLog("Config string '" + name + "' retrieved successfully (" + sections[1] + ").");
                        return sections[1].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    }

                    break;
                }
            }

            Logger.WriteToLog("Config strings '" + name + "' failed to read - reverting to default (" + fallback + ").");
            return fallback;
        }

        public static Color ReadColorFromHex(ICollection<string> lines, string name, Color fallback)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines.ElementAt(i);

                if (!line.StartsWith(".") && line.StartsWith(name))
                {
                    string[] sections = line.Split(new[] { '=' }, 2);

                    if (sections.Length == 2 && sections[1].StartsWith("0x"))
                    {
                        string hex = sections[1].Substring(2);
                        uint number = Convert.ToUInt32(hex, 16);

                        if (hex.Length == 6 || hex.Length == 8)
                        {
                            Color output = Color.FromArgb
                            (
                                hex.Length == 6 ? 255 : (int)(number >> 24),
                                (int)((number >> 16) & 255),
                                (int)((number >> 8) & 255),
                                (int)(number & 255)
                            );
                            Logger.WriteToLog("Config color '" + name + "' parsed successfully (" + hex + ").");
                            return output;
                        }

                        break;
                    }
                }
            }

            Logger.WriteToLog("Config color '" + name + "' failed to parse - reverting to default (" + fallback.ToString() + ").");
            return fallback;
        }
    }
}
