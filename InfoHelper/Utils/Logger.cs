using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.Utils
{
    public delegate void ErrorDel(string errorMessage);

    public static class Logger
    {
        public static event ErrorDel ErrorAdded;

        public static void AddRecord(string directory, string errorData)
        {
            try
            {
                string filePath = Path.Combine(directory, "Log.txt");

                FileStream fileStream = new FileStream(filePath, !File.Exists(filePath) ? FileMode.Create : FileMode.Append);

                StreamWriter sw = new StreamWriter(fileStream);

                sw.WriteLine($"Time: {DateTime.Now}. Error data: {errorData}");

                sw.Close();

                fileStream.Close();
            }
            finally
            {
                ErrorAdded?.Invoke(errorData);
            }
        }
    }
}
