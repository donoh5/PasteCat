using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace PasteCat
{
    class UserSettings
    {
        private IniFile ini;
        private bool startUp = false;

        public void GenerateINI()
        {
            try
            {
                ReadINI();
            }
            catch (FileNotFoundException)
            {
                WriteINI();
                ReadINI();
            }
        }

        private void ReadINI()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = Path.GetDirectoryName(path) + "\\userSettings.ini";

            ini = new IniFile();

            ini.Load(path);

            string startUpStr = ini["START_SET"]["STARTUP"].ToString();

            startUp = startUpStr == "TRUE";
        }

        private void WriteINI()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = Path.GetDirectoryName(path) + "\\userSettings.ini";

            ini = new IniFile();

            ini["START_SET"]["STARTUP"] = startUp.ToString().ToUpper();
            ini.Save(path, FileMode.OpenOrCreate);
        }

        public void SetStartUp()
        {
            using (RegistryKey runRegKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (runRegKey.GetValue("PasteCat") == null)
                {
                    runRegKey.SetValue("PasteCat", System.Windows.Forms.Application.ExecutablePath.ToString());
                }

                runRegKey.Close();
                startUp = true;

                WriteINI();
            }
        }

        public void CancelStartUp()
        {
            using (RegistryKey runRegKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (runRegKey.GetValue("PasteCat") != null)
                {
                    runRegKey.DeleteValue("PasteCat", false);
                }

                runRegKey.Close();
                startUp = false;

                WriteINI();
            }
        }
    }
}
