using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace MSBuild.CSharp.DefineConstants
{
    public class DefineConstantsTask : Task
    {
        public DefineConstantsTask()
        {
            Namespace = "Preprocessor";
            ClassName = "DefineConstants";
        }

        public override bool Execute()
        {
            try
            {
                string targetPath = TargetAssembly.ItemSpec;
                string targetDir = Path.GetDirectoryName(targetPath);
                string targetFile = Path.GetFileName(targetPath);
                string targetFileBase = Path.GetFileNameWithoutExtension(targetPath);

                AssemblyName aName = new AssemblyName(targetFileBase);
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave, targetDir);

                ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, targetFile);
                TypeBuilder tb = mb.DefineType($"{Namespace}.{ClassName}", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

                // Create properties
                foreach (ITaskItem i in DefineConstants)
                {
                    CreateProperty(i, tb);
                }

                if (!Log.HasLoggedErrors)
                {
                    Log.LogMessage(MessageImportance.High, $"Creating constants assembly '{targetPath}'");
                    Type t = tb.CreateType();
                    ab.Save(targetFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
            }
            return !Log.HasLoggedErrors;
        }

        private readonly Regex identifierRegex_ = new Regex("^[a-z_][a-z0-9_]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Dictionary<string, string> properties_ = new Dictionary<string, string>();
        private void CreateProperty(ITaskItem item, TypeBuilder typeBuilder)
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

            // Warn on duplicate
            if (properties_.ContainsKey(key))
            {
                if (properties_[key] != val)
                {
                    Log.LogWarning($"Property '{key}' was already defined. Existing value is '{properties_[key]}'. New value is '{val}'. Ignoring the new value");
                }
                return;
            }

            PropertyBuilder pb = typeBuilder.DefineProperty(key, PropertyAttributes.None, typeof(string), null);
            MethodAttributes getAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static;
            MethodBuilder mbGetAccessor = typeBuilder.DefineMethod($"get_{key}", getAttr, typeof(string), Type.EmptyTypes);
            ILGenerator getIL = mbGetAccessor.GetILGenerator();
            getIL.Emit(OpCodes.Ldstr, val);
            getIL.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbGetAccessor);
            Log.LogMessage(MessageImportance.Low, $"Created constant property '{key}'='{val}'");

            properties_.Add(key, val);
        }

        [Required]
        public ITaskItem[] DefineConstants { get; set; }

        [Required]
        public ITaskItem TargetAssembly { get; set; }

        public string Namespace { get; set; }

        public string ClassName { get; set; }
    }
}