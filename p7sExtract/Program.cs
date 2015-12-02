using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace p7sExtract
{
    static class Program
    {
        static void Main(string[] args)
        {
            string input = "";
            int totalConverted = 0;
            Console.WriteLine("p7s Extractor - creat de Radu Cosma\n");

            switch (args.Length)
            {
                case 0:
                    input = AppDomain.CurrentDomain.BaseDirectory;
                    break;
                case 1:
                    input = args[0];
                    break;
                default:
                    ShowHelp();
                    break;
            }

            if (File.Exists(input) && Path.GetExtension(input) == ".p7s")
            {
                input = Path.GetFullPath(input);
                ExtractP7S(input);
            }
            else if (Directory.Exists(input))
            {
                string[] inputFiles = Directory.GetFiles(input, "*.p7s");
                if (inputFiles.Length > 0)
                {
                    foreach (var inputFile in inputFiles)
                    {
                        totalConverted += ExtractP7S(inputFile);
                    }
                    Console.WriteLine("S-au salvat in total {0} fisiere.", totalConverted);
                }
                else { Console.WriteLine("Nu au fost gasite fisiere p7s in directorul ales."); }
            }
            else
            {
                Console.WriteLine("Fisierul/Directorul nu este valid: " + input);
                ShowHelp();
            }
            Console.WriteLine("\nApasa orice tasta pentru a inchide...");
            Console.ReadKey();
        }
        
        private static int ExtractP7S(string P7Sfile)
        {
            string output = Path.GetDirectoryName(P7Sfile) + "\\" + Path.GetFileNameWithoutExtension(P7Sfile);

            if (File.Exists(output))
            {
                Console.WriteLine("Fisierul exista deja: " + Path.GetFileNameWithoutExtension(P7Sfile));
                return 0;
            }
            else
            {
                using (BinaryReader binStream = new BinaryReader(File.OpenRead(P7Sfile)))
                {
                    ushort magicBytes = binStream.ReadUInt16();
                    long EOD = binStream.BaseStream.Length; //End of Data

                    switch (magicBytes)
                    {
                        case 0x8030:
                            binStream.BaseStream.Position = 50;
                            //EOD -= 2195;
                            break;
                        case 0x8230:
                            binStream.BaseStream.Position = 58;
                            break;
                        case 0x8330:
                            binStream.BaseStream.Position = 63;
                            break;
                        default:
                            Console.WriteLine("Fisier incompatibil: " + Path.GetFileNameWithoutExtension(P7Sfile));
                            return 0;
                    }

                    Console.Write("Se salveaza " + Path.GetFileNameWithoutExtension(P7Sfile));
                    using (BinaryWriter outStream = new BinaryWriter(File.Open(output, FileMode.Create)))
                    {
                        bool end = false;
                        while (binStream.BaseStream.Position < EOD && !end)
                        {
                            byte blockHeader = binStream.ReadByte();

                            switch (blockHeader)
                            {
                                case 0x04:
                                    int blockLength = binStream.ReadByte();

                                    switch (blockLength)
                                    {
                                        case 0x81:
                                            blockLength = binStream.ReadByte();
                                            break;
                                        case 0x82:
                                            blockLength = binStream.ReadBigUInt16();
                                            if (blockLength < 1024) { end = true; }
                                            break;
                                        case 0x83:
                                            blockLength = binStream.ReadBigInt24();
                                            if (blockLength < 0xFFFFFF) { end = true; }
                                            break;
                                        default:
                                            end = true;
                                            break;
                                    }

                                    if (blockLength < EOD - binStream.BaseStream.Position)
                                    {
                                        byte[] block = binStream.ReadBytes(blockLength);
                                        outStream.Write(block);
                                    }
                                    else { end = true; }
                                    break;
                                default:
                                    end = true;
                                    break;
                            }
                        }
                    }

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine("S-a salveat " + Path.GetFileNameWithoutExtension(P7Sfile));
                    return 1;
                }
            }
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine("\nMod de utilizare:\np7sExtract.exe [nume_fisier / nume_director]\nIn cazul in care nu este specificat un fisier sau director, vor fi procesate toate fisierele .p7s din directorul curent.\nFisierele existente nu vor fi suprascrise.");
        }

        private static ushort ReadBigUInt16(this BinaryReader bStream)
        {
            var bytes = bStream.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static int ReadBigInt24(this BinaryReader bStream)
        {
            var bytes = bStream.ReadBytes(3);
            return ((bytes[0] << 16) + (bytes[1] << 8) + bytes[2]);
        }

        private static int ReadBigInt32(this BinaryReader bStream)
        {
            var bytes = bStream.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
