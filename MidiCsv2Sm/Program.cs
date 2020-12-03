using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiCsv2Sm
{
    class Program
    {


        static void Main(string[] args)
        {
            

            foreach (var fileName in args)
            {
                Convert(fileName);
            }

            
        }

        static void Convert(string fileName)
        {
            Decoder decoder = new Decoder();

            string fileNameWithoutExt = fileName.Split('.')[0];


            try
            {
                String line;
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(fileName);
                //Read the first line of text
                line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {
                    decoder.Parse(line);
                    //write the lie to console window
                    //Console.WriteLine(line);
                    //Read the next line
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }

            decoder.CleanSuspendedChannelEvents();
            decoder.SortChannelEvents();

            List<string> encodedLines = decoder.Encode();

            try
            {

                StreamWriter sw = new StreamWriter(fileNameWithoutExt + "_Converted.csv");

                foreach (var line in encodedLines)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }


            Console.WriteLine("ok.");
            //Console.Read();

            /*
            List<int> list = new List<int>();

            int a = 5, b = 10;
            list.Add(a);
            list.Add(b);

            list.Sort((x, y) =>
            {

                if (x > y)
                    return 1;
                else
                    return -1;

            });

            Console.WriteLine(list[0] + "," + list[1]);
            Console.Read();
            */

        }
    }
}
