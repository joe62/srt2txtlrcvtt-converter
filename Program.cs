using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace srt2txtlrc_converter
{
    class Program
    {
        private static int _nOffsetDirection = 1;
        private static  uint _offsetMs =0;
        static void Main(string[] args)
        {
            string dir = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            ListFiles(new DirectoryInfo(dir));
            Console.WriteLine("回车退出...");
            Console.ReadLine();
        }

        private static void ListFiles(FileSystemInfo info)
        {
            
            if (!info.Exists)
                return;

            DirectoryInfo dir = info as DirectoryInfo;
            //不是目录 
            if (dir == null)
                return;

            FileSystemInfo[] files = dir.GetFileSystemInfos();
            var srtFiles = files.Where(p => p.Extension == ".srt");
            StringBuilder sb = new StringBuilder();
            foreach (FileInfo file in srtFiles)
            {
                if (sb.Length == 0)
                    sb.AppendLine(file.DirectoryName);
                sb.AppendLine(file.Name + "\t " + file.Length);

                Convert(file.FullName);
            }
            if(sb.Length>0)
                Console.WriteLine(sb);
            var dirs = files.Where(p => p.Attributes == FileAttributes.Directory);
            foreach (var item in dirs)
            {
                ListFiles(item);
            }
        }
        private static void Convert(string sFilePath)
        {
            using (var strReader = new StreamReader(sFilePath))
            using (var strWriter = new StreamWriter(sFilePath.Replace(".srt", ".vtt")))
            using (var strWriterLrc = new StreamWriter(sFilePath.Replace(".srt", ".lrc")))
            using (var strWriterTxt = new StreamWriter(sFilePath.Replace(".srt", ".txt")))
            {
                var rgxDialogNumber = new Regex(@"^\d+$");
                var rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d,\d\d\d) --> (\d\d:\d\d:\d\d,\d\d\d)");

                // Write starting line for the WebVTT file
                strWriter.WriteLine("WEBVTT");
                strWriter.WriteLine("");
   
                // Handle each line of the SRT file
                string sLine;
                
                while ((sLine = strReader.ReadLine()) != null)
                {
                    // We only care about lines that aren't just an integer (aka ignore dialog id number lines)
                    if (rgxDialogNumber.IsMatch(sLine))
                        continue;

                    // If the line is a time frame line, reformat and output the time frame
                    Match match = rgxTimeFrame.Match(sLine);
                    
                    if (match.Success)
                    {
                        string sLineMp3 = "";
                        if (_offsetMs > 0)
                        {
                            // Extract the times from the matched time frame line
                            var tsStartTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                            var tsEndTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                            // Modify the time with the offset
                            long startTimeMs = _nOffsetDirection * _offsetMs + (uint)tsStartTime.TotalMilliseconds;
                            long endTimeMs = _nOffsetDirection * _offsetMs + (uint)tsEndTime.TotalMilliseconds;
                            tsStartTime = TimeSpan.FromMilliseconds(startTimeMs < 0 ? 0 : startTimeMs);
                            tsEndTime = TimeSpan.FromMilliseconds(endTimeMs < 0 ? 0 : endTimeMs);

                            // Construct the new time frame line
                            sLine = tsStartTime.ToString(@"hh\:mm\:ss\.fff") +
                                    " --> " +
                                    tsEndTime.ToString(@"hh\:mm\:ss\.fff");

                            // Construct the mp3
                            sLineMp3 = "\r\n[" + tsStartTime.ToString(@"mm\:ss\.ff") + "]";
                        }
                        else
                        {
                            sLine = sLine.Replace(',', '.'); // Simply replace the comma in the time with a period
                            var tsStartTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                            sLineMp3 = "\r\n[" + tsStartTime.ToString(@"mm\:ss\.ff") + "]";
                        }

                        if(sLineMp3.Length>0)
                            strWriterLrc.Write(sLineMp3);
                    }
                    else if (!string.IsNullOrWhiteSpace(sLine))
                    {
                        if (sLine.Last() == '.')
                        { strWriterTxt.WriteLine(sLine);
                            
                        }
                            
                        else
                        {
                            strWriterTxt.Write(sLine + " ");
                            
                        }
                        strWriterLrc.Write(sLine+" ");
                    }

                    strWriter.WriteLine(sLine); // Write out the line
                    
                    
                }
            }
        }
    }
}
