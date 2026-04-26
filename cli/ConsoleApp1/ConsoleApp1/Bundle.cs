using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CodeBundler
{
    class Bundle
    {
        static void Main(string[] args)
        {
            // ==========================================
            // פענוח אוטומטי של קובץ RSP (אם המשתמש הזין @file.rsp)
            // ==========================================
            if (args.Length == 1 && args[0].StartsWith("@"))
            {
                string rspPath = args[0].Substring(1);
                if (File.Exists(rspPath))
                {
                    string rspContent = File.ReadAllText(rspPath);
                    var parsedArgs = new List<string>();
                    bool inQuotes = false;
                    string currentArg = "";

                    foreach (char c in rspContent)
                    {
                        if (c == '"') inQuotes = !inQuotes;
                        else if (char.IsWhiteSpace(c) && !inQuotes)
                        {
                            if (currentArg.Length > 0)
                            {
                                parsedArgs.Add(currentArg);
                                currentArg = "";
                            }
                        }
                        else currentArg += c;
                    }
                    if (currentArg.Length > 0) parsedArgs.Add(currentArg);
                    args = parsedArgs.ToArray();
                }
            }

            // ==========================================
            // 1. הגדרת פקודת bundle והאפשרויות שלה (ללא AddAlias כדי למנוע שגיאות בגרסה זו)
            // ==========================================
            var bundleCommand = new Command("bundle", "Bundle code files into a single file");

            var languageOption = new Option<string[]>("--language");
            languageOption.Description = "List of programming languages or 'all'";
            languageOption.Required = true;

            var outputOption = new Option<FileInfo>("--output");
            outputOption.Description = "Output file path";

            var noteOption = new Option<bool>("--note");
            noteOption.Description = "Include source file origin as a comment";

            var sortOption = new Option<string>("--sort");
            sortOption.Description = "Sort by 'name' or 'type'";

            var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines");
            removeEmptyLinesOption.Description = "Remove empty lines from source code";

            var authorOption = new Option<string>("--author");
            authorOption.Description = "Author name to include in header";

            bundleCommand.Add(languageOption);
            bundleCommand.Add(outputOption);
            bundleCommand.Add(noteOption);
            bundleCommand.Add(sortOption);
            bundleCommand.Add(removeEmptyLinesOption);
            bundleCommand.Add(authorOption);

            // ==========================================
            // 2. הגדרת פקודת create-rsp
            // ==========================================
            var createRspCommand = new Command("create-rsp", "Interactive prompt to create a response file (.rsp)");

            var rootCommand = new RootCommand("CLI Tool for bundling code");
            rootCommand.Add(bundleCommand);
            rootCommand.Add(createRspCommand);

            // ==========================================
            // 3. ניתוח (Parse) ובחירת הלוגיקה
            // ==========================================
            var parseResult = rootCommand.Parse(args);

            // הצגת עזרת משתמש. הסרנו את בדיקת השגיאות הפנימית של הספרייה כדי שכינויים מקוצרים יעבדו חלק.
            if (args.Contains("--help") || args.Contains("-h") || args.Length == 0)
            {
                Console.WriteLine("Usage: bundle [options] OR create-rsp");
                Console.WriteLine("\nBundle Options:");
                Console.WriteLine("  -l, --language           [Required] List of programming languages (e.g., cs js) or 'all'");
                Console.WriteLine("  -o, --output             [Required] Output file name or full path");
                Console.WriteLine("  -n, --note               Include source file path as a comment");
                Console.WriteLine("  -s, --sort               Sort by 'name' or 'type'");
                Console.WriteLine("  -r, --remove-empty-lines Remove empty lines from the source code");
                Console.WriteLine("  -a, --author             Add author name at the top of the bundle");
                return;
            }

            string commandName = parseResult.CommandResult.Command.Name;

            if (commandName == "create-rsp" || args.Contains("create-rsp"))
            {
                RunCreateRspInteractive();
            }
            else // ברירת מחדל לאריזה
            {
                RunBundleCommand(args);
            }
        }

        // ==========================================
        // לוגיקת פקודת create-rsp (אינטראקטיבי)
        // ==========================================
        static void RunCreateRspInteractive()
        {
            var rspCommand = new List<string> { "bundle" };

            Console.WriteLine("--- RSP File Creator ---");
            Console.WriteLine("Follow the prompts to configure your bundle command.\n");

            string language;
            while (true)
            {
                Console.Write("1. Enter languages to bundle (e.g., 'cs js' or 'all') [Required]: ");
                language = Console.ReadLine()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(language)) break;
                Console.WriteLine("   -> Error: Language cannot be empty. Please try again.");
            }
            rspCommand.Add("--language");
            rspCommand.Add(language);

            string output;
            while (true)
            {
                Console.Write("2. Enter output file name or path [Required]: ");
                output = Console.ReadLine()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(output)) break;
                Console.WriteLine("   -> Error: Output path cannot be empty. Please try again.");
            }
            rspCommand.Add("--output");
            rspCommand.Add($"\"{output}\"");

            Console.Write("3. Include source file origin as comments? (Y/N): ");
            string noteAns = Console.ReadLine()?.Trim().ToUpper() ?? "";
            if (noteAns == "Y")
            {
                rspCommand.Add("--note");
            }

            Console.Write("4. Sort files by 'name' or 'type' (Press Enter for default 'name'): ");
            string sort = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (sort == "type")
            {
                rspCommand.Add("--sort");
                rspCommand.Add("type");
            }

            Console.Write("5. Remove empty lines from source code? (Y/N): ");
            string removeAns = Console.ReadLine()?.Trim().ToUpper() ?? "";
            if (removeAns == "Y")
            {
                rspCommand.Add("--remove-empty-lines");
            }

            Console.Write("6. Enter author name (Press Enter to skip): ");
            string author = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(author))
            {
                rspCommand.Add("--author");
                rspCommand.Add($"\"{author}\"");
            }

            Console.WriteLine("\n--------------------------------");
            Console.Write("Enter the name for the response file (Press Enter for 'response.rsp'): ");
            string rspFileName = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(rspFileName)) rspFileName = "response.rsp";
            if (!rspFileName.EndsWith(".rsp")) rspFileName += ".rsp";

            try
            {
                File.WriteAllText(rspFileName, string.Join(" ", rspCommand));
                Console.WriteLine($"\nSUCCESS: Response file created at '{rspFileName}'");
                string exeName = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine($"To use it, run your app like this:  {exeName} @{rspFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFailed to save RSP file: {ex.Message}");
            }
        }

        // ==========================================
        // לוגיקת פקודת bundle (אריזת הקבצים)
        // ==========================================
        static void RunBundleCommand(string[] args)
        {
            try
            {
                var arguments = ParseArguments(args);

                string langRaw = GetArg(arguments, "--language", "-l");
                string outputRaw = GetArg(arguments, "--output", "-o");

                if (string.IsNullOrEmpty(langRaw) || string.IsNullOrEmpty(outputRaw))
                {
                    Console.WriteLine("Error: --language and --output are required.");
                    return;
                }

                var languages = langRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var output = new FileInfo(outputRaw);
                bool note = arguments.ContainsKey("--note") || arguments.ContainsKey("-n");
                string sort = GetArg(arguments, "--sort", "-s") ?? "name";
                bool removeEmpty = arguments.ContainsKey("--remove-empty-lines") || arguments.ContainsKey("-r");
                string author = GetArg(arguments, "--author", "-a");

                var currentDir = Directory.GetCurrentDirectory();

                var allFiles = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var lowerPath = f.ToLower();
                        return !lowerPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !lowerPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                               !lowerPath.Contains($"{Path.DirectorySeparatorChar}debug{Path.DirectorySeparatorChar}");
                    })
                    .ToList();

                var filteredFiles = allFiles.Where(file =>
                {
                    if (langRaw.ToLower() == "all") return true;
                    var ext = Path.GetExtension(file).TrimStart('.').ToLower();
                    return languages.Any(l => l.Trim().ToLower() == ext);
                }).ToList();

                // 1. בדיקה אם בכלל נמצאו קבצים לאריזה
                if (filteredFiles.Count == 0)
                {
                    Console.WriteLine("Warning: No files were found matching the specified languages.");
                    return;
                }

                // 2. בדיקה אם תיקיית היעד קיימת
                var outputDir = output.Directory;
                if (outputDir != null && !outputDir.Exists)
                {
                    Console.WriteLine($"Error: The destination directory '{outputDir.FullName}' does not exist.");
                    return;
                }

                // 3. התראה על דריסת קובץ קיים עם בקשת אישור
                if (output.Exists)
                {
                    Console.Write($"Warning: The file '{output.Name}' already exists. Overwrite? (Y/N): ");
                    string ans = Console.ReadLine()?.Trim().ToUpper() ?? "";
                    if (ans != "Y")
                    {
                        Console.WriteLine("Operation cancelled.");
                        return;
                    }
                }

                if (sort.ToLower() == "type")
                    filteredFiles = filteredFiles.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f)).ToList();
                else
                    filteredFiles = filteredFiles.OrderBy(f => Path.GetFileName(f)).ToList();

                using (var writer = new StreamWriter(output.FullName))
                {
                    if (!string.IsNullOrEmpty(author))
                    {
                        writer.WriteLine($"// Author: {author}");
                        writer.WriteLine("// " + new string('-', 20));
                    }

                    foreach (var file in filteredFiles)
                    {
                        if (Path.GetFullPath(file) == Path.GetFullPath(output.FullName)) continue;

                        if (note) writer.WriteLine($"// Source: {Path.GetFileName(file)} | Path: {Path.GetRelativePath(currentDir, file)}");

                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            if (removeEmpty && string.IsNullOrWhiteSpace(line)) continue;
                            writer.WriteLine(line);
                        }
                        writer.WriteLine();
                    }
                }
                Console.WriteLine($"Successfully created bundle: {output.Name} with {filteredFiles.Count} files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bundling: {ex.Message}");
            }
        }

        // --- פונקציות עזר בטוחות לחילוץ הנתונים ---
        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "bundle" || args[i] == "create-rsp") continue;

                if (args[i].StartsWith("-") && i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    dict[args[i]] = args[i + 1];
                    i++;
                }
                else if (args[i].StartsWith("-"))
                {
                    dict[args[i]] = "true";
                }
            }
            return dict;
        }

        static string GetArg(Dictionary<string, string> dict, string full, string shortName)
        {
            if (dict.TryGetValue(full, out var val)) return val;
            if (dict.TryGetValue(shortName, out var valShort)) return valShort;
            return "";
        }
    }
}