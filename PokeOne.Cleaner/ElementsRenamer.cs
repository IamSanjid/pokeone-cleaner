using Mono.Cecil;
using PROShine.Cleaner.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PROShine.Cleaner
{
    public class ElementsRenamer
    {
        public Dictionary<string, string> RenamedClasses { get; } = new Dictionary<string, string>();

        private static readonly Regex ObfuscatedName = new Regex("^[A-Z0-9]{11}$");

        private readonly AssemblyDefinition assembly;

        public readonly UnityBundle UnityBundle;

        private int classCount;
        private int propertyCount;
        private int fieldCount;
        private int methodCount;
        private int paramCount;

        public ElementsRenamer(AssemblyDefinition assembly, UnityBundle bundle)
        {
            this.assembly = assembly;
            UnityBundle = bundle;
        }

        public void Execute()
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                RenameElements(module);
            }
        }

        private void RenameElements(ModuleDefinition module)
        {
            foreach (TypeDefinition type in module.Types)
            {
                RenameElements(type);
            }
        }

        public bool DoesTheBundleContains(string stringStuff)
        {
            var sizedStringStyff = Encoding.Default.GetString(BitConverter.GetBytes(stringStuff.Length)) + stringStuff;
            bool result = false;
            for (int i = 0; i < 2; ++i)
            {
                int index = UnityBundle.ContentData.IndexOf(sizedStringStyff);
                result = index > 0;
            }
            return result;
        }

        private void RenameElements(TypeDefinition type)
        {

            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                RenameElements(nestedType);
            }

            if (!ObfuscatedName.IsMatch(type.Name)) return;
            if (type.IsAbstract || type.IsInterface || type.GenericParameters.Count > 0 || !type.IsClass || type.IsEnum) return;

            classCount += 1;

            string newClassName = RetriveClassName(type.Name);

            if (string.IsNullOrEmpty(newClassName))
                newClassName = "Class" + classCount;

            Console.WriteLine("Renaming class " + type.Name + " to " + newClassName);
            RenamedClasses.Add(type.Name, newClassName);

            type.Name = newClassName;

            foreach (FieldDefinition field in type.Fields)
            {
                RenameField(field);
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                RenameProperty(property);
            }

            foreach (MethodDefinition method in type.Methods)
            {
                RenameMethod(method);

                if (!method.HasParameters) continue;

                paramCount = 0;
                foreach (ParameterDefinition parameter in method.Parameters)
                {
                    RenameParameter(parameter);
                }
            }
        }

        private void RenameMethod(MethodDefinition method)
        {
            if (!ObfuscatedName.IsMatch(method.Name)) return;

            if (method.IsVirtual || method.IsSetter || method.IsGetter || method.IsVirtual || method.IsAbstract ||
                method.HasOverrides || method.IsCompilerControlled || method.IsConstructor || method.IsPInvokeImpl ||
                method.IsUnmanaged || method.IsUnmanagedExport) return;

            // This will only work for PokeOne
            if (method.HasThis && method.FullName.Contains("System.Void") &&
                !method.FullName.Contains("PSXAPI")
                && !method.FullName.Contains("UnityEngine") 
                && method.HasParameters && method.Parameters.Any(x => ContainsSystemStrin(x.ParameterType.Name))
                && !method.Parameters.Any(x => x.ParameterType.Name.Contains("String")) 
                && !method.Parameters.Any(x => x.ParameterType.Name.Contains("Single"))
                && !method.Parameters.Any(x => x.ParameterType.Name.Contains("Int"))
                && method.Parameters.Count == 1 && !method.FullName.Contains("/"))
            {
                return;
            }

            methodCount += 1;
            string newMethodName = "Method" + methodCount;

            Console.WriteLine("Renaming method " + method.Name + " to " + newMethodName);
            method.Name = newMethodName;
        }

        private bool ContainsSystemStrin(string st)
        {
            return st.Contains("Boolean");
        }

        private void RenameField(FieldDefinition field)
        {
            if (!ObfuscatedName.IsMatch(field.Name)) return;

            fieldCount += 1;
            string newFieldName = "field" + fieldCount;

            Console.WriteLine("Renaming field " + field.Name + " to " + newFieldName);
            field.Name = newFieldName;
        }

        private void RenameProperty(PropertyDefinition field)
        {
            if (!ObfuscatedName.IsMatch(field.Name)) return;

            propertyCount += 1;
            string newFieldName = "Property" + propertyCount;

            Console.WriteLine("Renaming property " + field.Name + " to " + newFieldName);
            field.Name = newFieldName;
        }

        private void RenameParameter(ParameterReference parameter)
        {
            if (!ObfuscatedName.IsMatch(parameter.Name)) return;

            paramCount += 1;
            string newParamName = "param" + paramCount;

            Console.WriteLine("Renaming parameter " + parameter.Name + " to " + newParamName);
            parameter.Name = newParamName;
        }

        private string RetriveClassName(string oldName)
        {
            var texts = File.ReadAllText("poke1_class.txt");
            var classNames = texts.Split('\n');
            var newClassName = "";
            foreach(var className in classNames)
            {
                if (oldName == className.Split('=')[0])
                    newClassName = className.Split('=')[1].Trim();
            }
            return newClassName;
        }
    }
}
