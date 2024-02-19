using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Checksum_Utility
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;
            while (true)
            {
                ChecksumGenerator.PrintCommands();

                try
                {
                    var gen = new ChecksumGenerator();
                    gen.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine('\n' + e.ToString());
                    break;
                }
            }

            Console.WriteLine("\nPress any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }

    public class ChecksumGenerator  
    {
        public void Run()
        {
            string? command = Console.ReadLine();
            if (!TryParseCommand(command, out HashAlgorithm? algorithm, out string? algorithmArg, out string? input, out bool isDirectory))
            {
                Console.WriteLine("Invalid Command");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string[] files = isDirectory ? Directory.GetFiles(input, "*", SearchOption.AllDirectories) : new string[] { input };

            Console.WriteLine($"Hash {files.Length} files? Y/N");
            string? line = Console.ReadLine();
            if (line == null || (!line.Equals("y", StringComparison.OrdinalIgnoreCase) && !line.Equals("yes", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            StringBuilder strb = new StringBuilder();
            strb.AppendLine($"Hash Algorithm: {algorithmArg}\n");
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                using FileStream fs = File.OpenRead(file);
                string hash = Convert.ToHexString(algorithm.ComputeHash(fs)).ToLowerInvariant();
                strb.AppendLine(hash + "    " + file);
                Console.WriteLine($"{i + 1} of {files.Length} - {hash} {file}");
            }

            sw.Stop();

            string output = Path.Combine(isDirectory ? input : Path.GetDirectoryName(input)!, "checksum.txt");
            int ii = 0;
            while (File.Exists(output))
            {
                ii++;
                output = Path.Combine(Path.GetDirectoryName(output)!, $"checksum ({ii}).txt");
            }
            using StreamWriter outputStream = new StreamWriter(output);
            outputStream.Write(strb);
            outputStream.Flush();
            outputStream.Close();

            Console.WriteLine($"Finished hashing {files.Length} files");
            Console.WriteLine($"Hash Algorithm: {algorithmArg}");
            Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalMilliseconds} ms");
            Console.WriteLine($"Average time per file: {sw.Elapsed.TotalMilliseconds / files.Length} ms");
            Console.WriteLine($"Results: {output}");
            Console.WriteLine("\nPress any key to continue");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static bool TryParseCommand(string? command, [NotNullWhen(true)] out HashAlgorithm? hash, [NotNullWhen(true)] out string? hashArg, [NotNullWhen(true)] out string? input, out bool isDirectory)
        {
            hash = default;
            hashArg = default;
            input = default;
            isDirectory = default;

            if (String.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            string[] tempBuffer = command.Split('\"');
            List<string> args = new List<string>();
            for (int i = 0; i < tempBuffer.Length; i++)
            {
                // odd = inside double quotes
                if (String.IsNullOrEmpty(tempBuffer[i]))
                {
                    continue;
                }
                else if (i % 2 != 0)
                {
                    args.Add(tempBuffer[i].Trim());
                }
                else
                {
                    args.AddRange(tempBuffer[i].Trim().Split(' '));
                }
            }

            if (args.Count != 3 || !args[0].Equals("hash", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "md5": hash = MD5.Create(); break;
                case "sha1": hash = SHA1.Create(); break;
                case "sha256": hash = SHA256.Create(); break;
                case "sha384": hash = SHA384.Create(); break;
                case "sha512": hash = SHA512.Create(); break;
                default:
                    return false;
            };
            hashArg = args[1];

            input = args[2];
            if (!Path.Exists(input))
            {
                return false;
            }

            var attributes = File.GetAttributes(input);
            isDirectory = attributes.HasFlag(FileAttributes.Directory);

            return true;
        }

        public static void PrintCommands()
        {
            string commands =
@"checksum <hashAlgorithm> <inputPath>

Algorithms:
md5
sha1
sha256
sha384
sha512

inputPath can be either a folder or a file.
";
            Console.WriteLine(commands);
        }
    }
}
