using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Learning_CSharp
{
    class Program
    {
        static void MainTest(string[] args)
        {
            while (true)
            {
                string thing = LongToBase36(long.Parse(Console.ReadLine()));
                Console.Clear();
                Console.WriteLine(thing);
                Console.WriteLine(thing.Length);
            }

            Console.ReadKey();
        }


        static UriSource uriSource = UriSource.Random;
        static string before = "screenshot-image\" src=\"";
        static string outPath = @"E:\Media\Pictures\Lightshot\";
        static string hashesPath = outPath + "hashes.txt";
        static string namesPath = outPath + "names.txt";
        static string uriPath = outPath + "uri.txt";
        static string baseUrl = "https://prnt.sc/";
        static string[] uris = new string[]
        {
            "1rixz12", "1riy0ea", "1riy14b", "1riy2cd", "1riy2u8",
        };


        static HashSet<string> prevImgHashes = new HashSet<string>();
        static HashSet<string> prevImgNames = new HashSet<string>();
        static WebClient client = new WebClient();
        static SHA1 hasher = SHA1.Create();
        static Random rnd = new Random();
        static long currentUri = 1679616;
        static bool stop = false;
        static char[] rndStrChars = new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z'
        };


        static Action StopOnPress = new Action(() =>
        {
            while (!Console.KeyAvailable) { }
            stop = true;
        });


        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                switch(args[0].Trim().ToLower())
                {
                    case "help":
                        Console.WriteLine("Random, List, Sequence");
                        return;
                    case "random":
                        uriSource = UriSource.Random;
                        break;
                    case "list":
                        uriSource = UriSource.List;
                        break;
                    case "sequence":
                        uriSource = UriSource.Sequence;
                        break;
                }
            }

            if (File.Exists(hashesPath))
                prevImgHashes = File.ReadAllLines(hashesPath).ToHashSet();

            if (File.Exists(namesPath))
                prevImgNames = File.ReadAllLines(namesPath).ToHashSet();

            if (uriSource == UriSource.Sequence && File.Exists(uriPath))
            {
                if (long.TryParse(File.ReadAllText(uriPath), out long tempUri))
                    currentUri = tempUri;
            }

            new Task(StopOnPress).Start();

            SaveImages();

            File.WriteAllLines(hashesPath, prevImgHashes);
            File.WriteAllLines(namesPath, prevImgNames);
            if (uriSource == UriSource.Sequence)
                File.WriteAllText(uriPath, currentUri.ToString());

            Console.WriteLine("End");
            Console.ReadLine();

            client.Dispose();
            hasher.Dispose();
        }


        static void SaveImages()
        {
            if (uriSource == UriSource.List)
            {
                foreach (string uri in uris)
                {
                    if (stop)
                        break;

                    SaveImage(baseUrl + uri);
                }
            }
            else if (uriSource == UriSource.Random)
            {
                while (!stop)
                {
                    SaveImage(baseUrl + LongToBase36(RandomLong(1679616, 4353564672)));
                }
            }
            else if (uriSource == UriSource.Sequence)
            {
                for (; currentUri < 4353564672; currentUri++)
                {
                    if (stop)
                        break;

                    SaveImage(baseUrl + LongToBase36(currentUri));
                }

                currentUri--;
            }
        }


        static long RandomLong(long min, long max)
        {
            byte[] buf = new byte[8];
            rnd.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);
            return (Math.Abs(longRand % (max - min)) + min);
        }


        // 1679616 = 10000
        // 4353564671 = 1zzzzzz
        // 78364164095 = zzzzzzz
        static string LongToBase36(long value)
        {
            long i = 32;
            char[] buffer = new char[i];
            long targetBase = rndStrChars.Length;

            do
            {
                buffer[--i] = rndStrChars[value % targetBase];
                value = value / targetBase;
            }
            while (value > 0);

            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }


        static void SaveImage(string url)
        {
            Thread.Sleep(250);

            string imgName = url.Substring(16);
            if (prevImgNames.Contains(imgName))
                return;

            prevImgNames.Add(imgName);

            client.Headers.Add("User-Agent: Other");
            string html = client.DownloadString(url);

            // "The screenshot was removed"
            if (html.IndexOf("0_173a7b_211be8ff.png") != -1)
                return;

            int index = html.IndexOf(before) + before.Length;

            string htmlCut = html.Substring(index);
            string imgUrl = htmlCut.Substring(0, htmlCut.IndexOf("\""));

            byte[] imgBytes;
            try
            {
                imgBytes = client.DownloadData(imgUrl);
            }
            catch (Exception ex)
            {
                return;
            }

            byte[] hashBytes = hasher.ComputeHash(imgBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            string hash = sb.ToString();

            if (!prevImgHashes.Contains(hash))
            {
                prevImgHashes.Add(hash);

                using (MemoryStream ms = new MemoryStream(imgBytes))
                {
                    Image.FromStream(ms).Save(outPath + imgName + ".png");
                }

                Console.WriteLine(imgName + " saved");
            }
        }


        enum UriSource
        {
            Random,
            List,
            Sequence
        }
    }
}
