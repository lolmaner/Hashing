using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Hratky
{
    class Program
    {
        static void Main(string[] args)
        {
            var GCmemoryinfo = GC.GetGCMemoryInfo();
            long InstalledMemory = GCmemoryinfo.TotalAvailableMemoryBytes;
            var physicalMemory = (double)InstalledMemory / 1048576.0;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Total physical memory in MBs: \n" + physicalMemory);
            int max = 20;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\nNumber of logical processors:\n" + Environment.ProcessorCount); // prints the number of logical processors
            Console.ForegroundColor = ConsoleColor.White;
            var words = new ConcurrentDictionary<string, byte>(); // declaring list for detecting repetitions
            char[] chars = new char[26];
            for (int i = 0; i < 26; i++)
            {
                chars[i] = (char)('a' + i);
            }

            Console.WriteLine("\nWrite what you wanna hash:");
            string input = Console.ReadLine().ToLower();

            if (string.IsNullOrEmpty(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No word detected.");
                return;
            }

            if (input.Length > max)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nError: Input must be maximum {0} characters long.", max);
                return;
            }

            foreach (char item in input)
            {
                if (!CheckIfAlphabet(item))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nError: Input contains non-alphabetical characters.");
                    return;
                }
            }

            string hashedInput = Get16BitHashCode(input);
            Console.WriteLine("The hashed output is: " + hashedInput);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press any button to try to guess it");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey();

            
            int count = 0;
            int count2 = 0;
            string final = "";
            string finalguess = "";
            try
            {
                if (physicalMemory < MinimumMemory(input.Length))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Using a brute force method without repetition check, due to lack of memory!\nyou need at least {0} MB of memory to safely guess that", MinimumMemory(input.Length));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Press any button to continue");
                    Console.ForegroundColor = ConsoleColor.White;
                    ParallelOptions options = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount * 4 // Sets the number of threads for the parallel loop you can go higher than i set here lol
                    };
                    Console.ReadKey();
                    Parallel.For(0, int.MaxValue, options, (i, loopState) =>
                    {
                        string guess = "";
                        string temporary = "";
                        Random randomnumber = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount ^ i); // Unique seed for each thread

                        temporary = GenerateRandomString(randomnumber, chars, input.Length); // temporary = GenerateRandomString(randomnumber, chars, max)if you count the fact hat you wouldnt know the number of chars in the input



                        guess = Get16BitHashCode(temporary);

                        if (guess == hashedInput) // collision "detection" 
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine("Collision Found");
                            Console.ForegroundColor = ConsoleColor.White;
                            if (input == temporary)
                            {
                                finalguess = guess;
                                final = temporary;
                                loopState.Stop();
                            }
                        }
                        i--; // infinite loop LOL
                             // should probably remake this or i could just count collisions idk lol
                        if (count < 2147483647) // if one counter is full use the second one xd 
                        {
                            Interlocked.Increment(ref count);
                        }
                        else
                        {
                            Interlocked.Increment(ref count2);
                        }
                    });
                }
                else
                {
                    ParallelOptions options = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount * 8 // Sets the number of threads for the parallel loop you can go higher than i set here lol
                    };
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Using repetition checking method which should reduce CPU drag but is extremely costly for ram \n \nPress any button to continue");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.ReadKey();
                    Parallel.For(0, int.MaxValue, options, (i, loopState) =>
                    {
                        string guess = "";
                        string temporary = "";
                        Random randomnumber = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount ^ i); // Unique seed for each thread

                        temporary = GenerateRandomString(randomnumber, chars, input.Length); // temporary = GenerateRandomString(randomnumber, chars, max)if you count the fact hat you wouldnt know the number of chars in the input

                        if (!words.TryAdd(temporary, 0)) // checks if word isnt repetition, and adds it to the list if it isnt
                        {
                            return;
                        }
                        // this method of storing repetitions is extremely costly for ram xd needs better solution
                        guess = Get16BitHashCode(temporary);

                        if (guess == hashedInput) // collision "detection" 
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine("Collision Found");
                            Console.ForegroundColor = ConsoleColor.White;
                            if (input == temporary)
                            {
                                finalguess = guess;
                                final = temporary;
                                loopState.Stop();
                            }
                        }
                        i--; // infinite loop LOL
                             // should probably remake this or i could just count collisions idk lol
                        if (count < 2147483647) // if one counter is full use the second one xd 
                        {
                            Interlocked.Increment(ref count);
                        }
                        else
                        {
                            Interlocked.Increment(ref count2);
                        }
                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error somewhere :skull:");
                return;

            }

            

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finally found it! {0} and {1} match after {2} + {3} tries. The hash is: {4}", final, input, count, count2,finalguess);
            Console.ReadKey();
        }
        // Hashes string to 16 bit hash and returns it as string
        static string Get16BitHashCode(string input)
        {
            int hash = 5381;
            foreach (char item in input)
            {
                hash = ((hash << 5) + hash) + item;
            }
            short meow = (short)hash;
            return Convert.ToString(meow, 2).PadLeft(16, '0');
        }

        // checks if string has only alphabet symbols (lower case)
        static bool CheckIfAlphabet(char c)
        {
            return (c >= 'a' && c <= 'z');
        }
        // self explanatory
        static string GenerateRandomString(Random random, char[] chars, int max)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int ForLoopCount = max; // int forloopCount = random.Next(1,max + 1) if you count the fact hat you wouldnt know the number of chars in the input 
            for (int j = 0; j < ForLoopCount; j++)
            {
                char c = chars[random.Next(0, chars.Length)];
                stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }
        static int MinimumMemory(int length)
        {
            switch (length)
            {
                case 1:
                    return 1000;
                case 2:
                    return 2000;
                case 3:
                    return 4000;
                case 4:
                    return 4000;
                case 5:
                    return 15000;
                case 6:
                    return 15000;
                case > 11:
                    return 60000;
                case >6:
                    return 30000;
                default:
                    return 30000;
            }
        }
    }
}
