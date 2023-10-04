using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MSBuild.CSharp.DefineConstants
{
    public class DefineConstantsTask : Task
    {
        public DefineConstantsTask()
        {
            Namespace = "Preprocessor";
            ClassName = "DefineConstants";
            TargetPath = new TaskItem("DefineConstants.cs");
        }

        public override bool Execute()
        {
            try
            {
                string targetPath = TargetPath.ItemSpec;
                if (!Path.IsPathRooted(targetPath))
                {
                    targetPath = Path.Combine(Environment.CurrentDirectory, targetPath);
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"namespace {Namespace}");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine($"\tclass {ClassName}");
                stringBuilder.AppendLine("\t{");

                // Create properties
                foreach (ITaskItem i in DefineConstants)
                {
                    CreateProperty(i, stringBuilder);
                }

                stringBuilder.AppendLine("\t}");
                stringBuilder.AppendLine("}");

                if (!Log.HasLoggedErrors)
                {
                    using (TextWriter writer = new StreamWriter(targetPath))
                    {
                        writer.Write(stringBuilder.ToString());
                    }
                    CreatedSourceFile = new TaskItem(targetPath);
                    Log.LogMessage($"Created constants C# code file '{CreatedSourceFile.ItemSpec}'");
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
            }

            return !Log.HasLoggedErrors;
        }

        private enum PropertyType
        {
            String,
            StringArray,
            Bool,
            Int
        }

        private readonly Regex identifierRegex_ = new Regex("^[a-z_][a-z0-9_]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Dictionary<string, string> properties_ = new Dictionary<string, string>();
        private void CreateProperty(ITaskItem item, StringBuilder stringBuilder)
        {
            string kv = item.ItemSpec;
            if (string.IsNullOrWhiteSpace(kv))
            {
                return;
            }

            string key = kv;
            string val = "";
            int eq = kv.IndexOf('=');
            if (eq >= 0)
            {
                key = kv.Substring(0, eq);
                val = kv.Substring(eq + 1);
            }

            // Validate property name.
            if (string.IsNullOrWhiteSpace(key))
            {
                Log.LogError($"Ilegal key-value pair '{kv}'");
                return;
            }
            if (!identifierRegex_.IsMatch(key))
            {
                Log.LogError($"'{key}' is not a valid C# identifier. Allowed identifiers begin with a letter or an underscore, and contain only letters, digits, and underscores");
                return;
            }

            // Property type
            PropertyType pt = PropertyType.String;
            string typeStr = item.GetMetadata("Type");
            if (!string.IsNullOrWhiteSpace(typeStr) && !Enum.TryParse(typeStr, out pt))
            {
                Log.LogWarning($"Property '{key}' has unsupported type '{typeStr}'. Supported types are: {Enum.GetNames(typeof(PropertyType)).Aggregate((all, a) => all + ',' + a)}");
                pt = PropertyType.String;
            }

            Type type = typeof(string);
            switch (pt)
            {
                case PropertyType.Bool:
                    type = typeof(bool);
                    if (!bool.TryParse(val, out bool b))
                    {
                        int n;
                        if (!int.TryParse(val, out n))
                        {
                            Log.LogError($"Can't use '{val}' as a boolean value as specified by 'Type' metadata for item '{key}'. Supported values are '{bool.TrueString}', '{bool.FalseString}', and numeric zero / non-zero values");
                            return;
                        }
                        b = (n != 0);
                        Log.LogWarning($"Using numeric '{val}' as a boolean '{b}' value");
                    }
                    val = b.ToString().ToLower();
                    break;

                case PropertyType.Int:
                    type = typeof(int);
                    if (!int.TryParse(val, out int i))
                    {
                        Log.LogError($"Can't use '{val}' as an integer value as specified by 'Type' metadata for item '{key}'");
                        return;
                    }
                    string val1 = i.ToString("D");
                    if (!int.TryParse(val1, out int j) || (i != j))
                    {
                        Log.LogError($"Conversion of integer {key}={val} to C# code results in '{val1}' which doesn't parse back to the same integer value");
                        return;
                    }
                    val = val1;
                    break;

                case PropertyType.StringArray:
                    type = typeof(string[]);
                    string[] elements = val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((elements == null) || (elements.Length == 0))
                    {
                        Log.LogError($"Can't use '{val}' as a string array as specified by 'Type' metadata for item '{key}'");
                        return;
                    }

                    foreach (string element in elements)
                    {
                        val += "\"" + element.Replace("\"", "\\\"") + "\", ";
                    }
                    val = "new string[]{ " + val + "}";
                    break;

                case PropertyType.String:
                default:
                    pt = PropertyType.String;
                    type = typeof(string);
                    val = "\"" + val.Replace("\"", "\\\"") + "\"";
                    break;
            }

            // Warn on duplicate
            if (properties_.ContainsKey(key))
            {
                if (properties_[key] != val)
                {
                    Log.LogWarning($"Property '{key}' was already defined. Existing value is '{properties_[key]}'. New value is '{val}'. Ignoring the new value");
                }
                return;
            }

            string line = $"\t\tpublic static {type.FullName} {key} => {val};";
            stringBuilder.AppendLine(line);

            properties_.Add(key, val);
        }

        [Required]
        public ITaskItem[] DefineConstants { get; set; }

        [Required]
        public ITaskItem TargetPath { get; set; }

        [Output]
        public ITaskItem CreatedSourceFile { get; set; }

        public string Namespace { get; set; }

        public string ClassName { get; set; }
    }
}