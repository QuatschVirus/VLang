using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using VLang.Compilers;
using VLang.Transpiler;
using VLang.InternalTypes;

namespace VLang
{
    static class CentralCompiler
    {
        public static string SourcePath { get; private set; } = "";
        public static string TargetPath { get; private set; } = "";

        public static Language TargetLanguage { get; private set; }

        static void Main(string[] args)
        {
            // Preconditions
            if (args.Length < 1) { Logging.ThrowRaw(Error.ArgumentError, "Not enough arguments given. Requires atleast one: Compiling source"); }
            SourcePath = args[0];
            if (!Path.Exists(SourcePath)) { Logging.ThrowRaw(Error.FileError, "Source path is invalid or does not exist. Please specify a valid path as the first argument"); }

            if (args.Length >= 2) {
                TargetPath = args[1];
                if (Directory.Exists(TargetPath)) { Logging.ThrowRaw(Error.FileError, "Target path is invalid or not a directory. Please specify a valid path as the second argument"); }
            }

            if (args.Length >= 3) { Flags.Setup(args[2]); }

            foreach (string path in Directory.GetFiles(SourcePath, "", SearchOption.AllDirectories)) {
                HandleFile(path);
            }
        }

        static void HandleFile(string path)
        {
            Language lang = LanguageHelper.Detect(path);

            if (lang.key == LanguageKey.VMETA) {
                using StreamReader sr = new(path);
                Metadata metadata = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build().Deserialize<Metadata>(sr.ReadToEnd());
                TargetLanguage = LanguageHelper.FromID(metadata.compile.language);
                if (!TargetLanguage.validTargetLang)
                {
                    Logging.Throw(Error.MetadataError, $"{TargetLanguage.id} is not a valid target language", -1, path);
                }
                TargetPath = metadata.compile.target;
                Flags.Setup(metadata.compile.flags);
            } else if (lang.validSourceLang)
            {
                if (lang.compiler == null)
                {
                    Logging.ThrowRaw(Error.InternalCompilerError, $"{lang.id} is defined as a valid source language, but has no attached compiler");
                    return;
                }
                string newPath = Path.Combine(TargetPath, Path.GetRelativePath(SourcePath, path));
                using StreamReader sr = new(path);
                InstructionTree tree = lang.compiler.Compile(sr.ReadToEnd());
                if (tree.Key != InstructionKey.Root)
                {
                    Logging.ThrowRaw(Error.InternalCompilerError, "Instruction tree root does not have the Root key");
                }
                if (tree.Branches.Length < 1)
                {
                    Logging.Warn(Warning.EmptyInstructionTree, "The instruction tree is empty besides the root node. This may indicate an empty file or a compiler issue", -1, path);
                }
                if (tree.Branches[tree.Branches.Length - 1].Key == InstructionKey.Meta)
                {
                    Logging.Warn()
                }
                if (tree.Branches.Length > 1)
                {
                    foreach (InstructionTree branch in tree.Branches)
                    {

                    }
                }
            }
        }
    }


    static class Flags
    {
        public static bool Verbose { get; private set; } = false;
        public static bool Debug { get; private set; } = false;

        public static void Setup(string flagstring)
        {
            Verbose = flagstring.ToLower().Contains('v');
            Debug = flagstring.ToLower().Contains('d');
        }
    }


    static class Logging
    {
        public static void WarnRaw(Warning type, string message)
        {
            WarnRaw(type.ToString(), message);
        }

        public static void WarnRaw(string type, string message)
        {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- COMPILER WARNING ---");
            Console.WriteLine(type);
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }

        public static void Warn(Warning type, string message, int line, string path)
        {
            if (line >= 0)
            {
                using StreamReader sr = new(path);
                WarnRaw(type, $"On line {line} of {path}\n{sr.ReadToEnd().Split("\n")[line]}\n{message}");
            } else
            {
                WarnRaw(type, $"In {path}\n{message}");
            }
        }

        /// <summary>
        /// Displays an error in console and terminates the compiler
        /// </summary>
        /// <param name="type">The type of the error, which infers its exit code. If the provided options don't fit th error you want to descrieb, use <c>Logging.ThrowCustom</c></param>
        /// <param name="message">The message to be displayed with the error. Use this to provide some information about the error</param>
        /// <param name="repositoryURL">Optional. Provide a URL for an internal issue to be reported to. Defaults to </param>

        public static void ThrowRaw(Error type, string message, string? repositoryURL = null)
        {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("--- COMPILER ERROR ---");
            Console.Error.WriteLine(type);
            Console.Error.WriteLine(message);
            if (type == Error.InternalCompilerError)
            {
                Console.Error.WriteLine("This is an internal error, meaning something went wrong within the compiler. Please report this error over at Github");
            }
            Console.ForegroundColor = prev;
            Environment.Exit((int)type);
        }

        public static void ThrowCustom(string type, string message, int code, bool displayInternal = false)
        {

        }

        public static void Throw(Error type, string message, int line, string path)
        {
            if (line >= 0)
            {
                using StreamReader sr = new(path);
                ThrowRaw(type, $"On line {line} of {path}\n{sr.ReadToEnd().Split("\n")[line]}\n{message}");
            }
            else
            {
                ThrowRaw(type, $"In {path}\n{message}");
            }
        }
    }

    enum Warning
    {
        Test,
        UnknownLanguage,
        EmptyInstructionTree
    }

    enum Error
    {
        Test,
        InternalCompilerError, // Not the users fault, always report to developers
        ArgumentError,
        FileError,
        MetadataError,
        CompilerError
    }


    readonly struct Language
    {
        public readonly LanguageKey key;
        public readonly string extension;
        public readonly string id;
        public readonly bool validSourceLang;
        public readonly bool validTargetLang;
        public readonly ICompiler? compiler;
        public readonly ITranspiler? transpiler;
        public readonly LanguageKey? targetLanguageOverrideKey;

        public Language(LanguageKey key, string extension, string id)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = false;
            this.validTargetLang = false;
            this.compiler = null;
            this.transpiler = null;
            this.targetLanguageOverrideKey = null;
        }

        public Language(LanguageKey key, string extension, string id, ICompiler compiler)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = true;
            this.validTargetLang = false;
            this.compiler = compiler;
            this.transpiler = null;
            this.targetLanguageOverrideKey = null;
        }

        public Language(LanguageKey key, string extension, string id, ITranspiler transpiler)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = false;
            this.validTargetLang = true;
            this.compiler = null;
            this.transpiler = transpiler;
            this.targetLanguageOverrideKey = null;
        }

        public Language(LanguageKey key, string extension, string id, ICompiler compiler, Language targetLanguageOverride)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = true;
            this.validTargetLang = false;
            this.compiler = compiler;
            this.transpiler = null;
            if (!targetLanguageOverride.validTargetLang)
            {
                Logging.ThrowRaw(Error.InternalCompilerError, $"{targetLanguageOverride.id} has been specified as a target language override, but it isn't a valid target language");
            }
            this.targetLanguageOverrideKey = targetLanguageOverride.key;
        }

        public Language(LanguageKey key, string extension, string id, ITranspiler transpiler, Language targetLanguageOverride)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = false;
            this.validTargetLang = true;
            this.compiler = null;
            this.transpiler = transpiler;
            if (!targetLanguageOverride.validTargetLang)
            {
                Logging.ThrowRaw(Error.InternalCompilerError, $"{targetLanguageOverride.id} has been specified as a target language override, but it isn't a valid target language");
            }
            this.targetLanguageOverrideKey = targetLanguageOverride.key;
        }

        public Language(LanguageKey key, string extension, string id, ICompiler compiler, ITranspiler transpiler, Language targetLanguageOverride)
        {
            this.key = key;
            this.extension = extension;
            this.id = id;
            this.validSourceLang = true;
            this.validTargetLang = false;
            this.compiler = compiler;
            this.transpiler = transpiler;
            if (!targetLanguageOverride.validTargetLang)
            {
                Logging.ThrowRaw(Error.InternalCompilerError, $"{targetLanguageOverride.id} has been specified as a target language override, but it isn't a valid target language");
            }
            this.targetLanguageOverrideKey = targetLanguageOverride.key;
        }
    }

    enum LanguageKey
    {
        Unknown,
        VL,
        VPP,
        VS,
        VY,
        VMETA,
        CS,
        CPP
    }

    static class LanguageHelper
    {
        // VL native langs
        public static readonly Language VL = new(LanguageKey.VL, ".vl", "V", new VLCompiler());
        public static readonly Language VPP = new(LanguageKey.VPP, ".vpp", "V++", new VPPCompiler());
        public static readonly Language VS = new(LanguageKey.VS, ".vs", "V#", new VSCompiler());
        public static readonly Language VY = new(LanguageKey.VY, ".vy", "Vy", new VYCompiler());
        public static readonly Language VMETA = new(LanguageKey.VMETA, ".vmeta", "Vmeta"); // metadata file for VLang family projects, YAML format

        // C family
        public static readonly Language CS = new(LanguageKey.CS, ".cs", "C#", new CSTranspiler());


        public static readonly Language[] languages = { VL, VPP, VS, VY, VMETA };

        public static Language Detect(string filename)
        {
            foreach (Language l in languages)
            {
                if (Path.GetExtension(filename) == l.extension)
                {
                    return l;
                }
            }

            Logging.WarnRaw(Warning.UnknownLanguage, $"No registered filetype for {Path.GetExtension(filename)} files found. Handeling as plain text file");
            return new(LanguageKey.Unknown, Path.GetExtension(filename), "Unknown");
        }

        public static Language FromID(string id)
        {
            foreach (Language l in languages)
            {
                if (id == l.id)
                {
                    return l;
                }
            }

            Logging.WarnRaw(Warning.UnknownLanguage, $"No registered filetype for {id} found. Handeling as plain text file");
            return new(LanguageKey.Unknown, "", id);
        }

        public static Language FromKey(LanguageKey key)
        {
            foreach(Language l in languages)
            {
                if (key == l.key)
                {
                    return l;
                }
            }

            Logging.ThrowRaw(Error.InternalCompilerError, $"The language key {key} has no associated language.");
            return new(key, "", "");
        }
    }

    readonly struct CompileData
    {
        public readonly string language;
        public readonly string target;
        public readonly string flags;

        public CompileData(string language, string target, string flags)
        {
            this.language = language;
            this.target = target;
            this.flags = flags;
        }
    }

    readonly struct Metadata
    {
        public readonly string name;
        public readonly string source;
        public readonly CompileData compile;

        public Metadata(string name, string source, CompileData compile)
        {
            this.name = name;
            this.source = source;
            this.compile = compile;
        }

        public Metadata()
        {
            this.name = "ProjectName";
            this.source = "src";

        }
    }
}