using System;
using Microsoft.Win32;
using System.Reflection;
using System.Security;

namespace ScanAgent
{
    public static class ProtocolRegistration
    {
        private const string PROTOCOL_NAME = "scan-agent";
        private const string PROTOCOL_DESCRIPTION = "URL:Scan Agent Protocol";

        /// <summary>
        /// Check if the scan-agent:// protocol is registered
        /// </summary>
        public static bool IsProtocolRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(PROTOCOL_NAME))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register the scan-agent:// protocol handler
        /// Requires administrator privileges
        /// </summary>
        public static bool RegisterProtocol()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;

                // Create main protocol key
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(PROTOCOL_NAME))
                {
                    if (key == null) return false;

                    key.SetValue("", PROTOCOL_DESCRIPTION);
                    key.SetValue("URL Protocol", "");
                }

                // Create DefaultIcon key
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(PROTOCOL_NAME + "\\DefaultIcon"))
                {
                    if (key == null) return false;
                    key.SetValue("", "\"" + exePath + "\",1");
                }

                // Create shell\open\command key
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(PROTOCOL_NAME + "\\shell\\open\\command"))
                {
                    if (key == null) return false;
                    key.SetValue("", "\"" + exePath + "\" \"%1\"");
                }

                Logger.Instance.Log("Protocol handler registered successfully");
                return true;
            }
            catch (SecurityException)
            {
                Logger.Instance.Log("Failed to register protocol: Administrator privileges required");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Failed to register protocol: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Unregister the scan-agent:// protocol handler
        /// Requires administrator privileges
        /// </summary>
        public static bool UnregisterProtocol()
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(PROTOCOL_NAME);
                Logger.Instance.Log("Protocol handler unregistered successfully");
                return true;
            }
            catch (SecurityException)
            {
                Logger.Instance.Log("Failed to unregister protocol: Administrator privileges required");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Failed to unregister protocol: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get the current protocol registration status as a string
        /// </summary>
        public static string GetRegistrationStatus()
        {
            if (IsProtocolRegistered())
            {
                try
                {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(PROTOCOL_NAME + "\\shell\\open\\command"))
                    {
                        if (key != null)
                        {
                            string command = key.GetValue("") as string;
                            return "Registered: " + command;
                        }
                    }
                }
                catch { }
                return "Registered (details unavailable)";
            }
            else
            {
                return "Not registered";
            }
        }
    }
}

