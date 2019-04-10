using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace MSBuild.CSharp.DefineConstants
{
    public class DefineConstantsTask : Task
    {
        public DefineConstantsTask()
        {
            Assembly = "DefineConstants";
            Namespace = "Preprocessor";
            ClassName = "DefineConstants";
        }

        public override bool Execute()
        {
            try
            {
                AssemblyName aName = new AssemblyName(Assembly);
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);

                string targetPath = FilePaths[0].ItemSpec;
                string targetFile = Path.GetFileName(targetPath);

                ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, targetFile);
                TypeBuilder tb = mb.DefineType($"{Namespace}.{ClassName}", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

                // Per property
                foreach (ITaskItem i in DefineConstants)
                {
                    CreateProperty(i, tb);
                }

                if (!Log.HasLoggedErrors)
                {
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
            if (string.IsNullOrWhiteSpace(key))
            {
                Log.LogError($"Ilegal key-value pair '{kv}'");
                return;
            }

            //TODO Validate property name.

            PropertyBuilder pb = typeBuilder.DefineProperty(key, PropertyAttributes.None, typeof(string), null);
            MethodAttributes getAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static;
            MethodBuilder mbNumberGetAccessor = typeBuilder.DefineMethod($"get_{key}", getAttr, typeof(string), Type.EmptyTypes);
            ILGenerator numberGetIL = mbNumberGetAccessor.GetILGenerator();
            numberGetIL.Emit(OpCodes.Ldstr, val);
            numberGetIL.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbNumberGetAccessor);
        }

        [Required]
        public ITaskItem[] DefineConstants { get; set; }

        [Required]
        public ITaskItem[] FilePaths { get; set; }

        public string Assembly { get; set; }

        public string Namespace { get; set; }

        public string ClassName { get; set; }
    }
}
