﻿using Mono.Cecil;
using PROShine.Cleaner.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PROShine.Cleaner
{
    internal class Program
    {
        internal static void Main()
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly("Assembly-CSharp.dll.original");
            var bundle = UnityBundle.ReadFromFile("globalgamemanagers.assets.original");
            new UnusedMethodsRemover(assembly, bundle).Execute();
            var renamer = new ElementsRenamer(assembly, bundle);
            renamer.Execute();
            assembly.Write("Assembly-CSharp.dll");
            RenameClassesInBundle(bundle, renamer.RenamedClasses);
            bundle.WriteToFile("globalgamemanagers.assets");
        }

        private static void RenameClassesInBundle(UnityBundle bundle, IDictionary<string, string> renamedClasses)
        {
            foreach (var pair in renamedClasses)
            {
                RenameClassInBundle(bundle, pair.Key, pair.Value);
            }
        }

        private static void RenameClassInBundle(UnityBundle bundle, string oldClassName, string newClassName)
        {
            // TODO: Rewrite this method in order to make it understandable by humans.

            string sizedOldClassName = Encoding.Default.GetString(BitConverter.GetBytes(oldClassName.Length)) + oldClassName;
            string sizedNewClassName = Encoding.Default.GetString(BitConverter.GetBytes(newClassName.Length)) + newClassName;

            for (int i = 0; i < 2; ++i)
            {
                int oldSize = bundle.ContentData.Length;
                int index = bundle.ContentData.IndexOf(sizedOldClassName);
                if (index > 0)
                {
                    var editedObjectInfo = bundle.Metadata.Objects.First(x => index >= x.DataOffset && index < x.DataOffset + x.DataSize);

                    string paddedOldClassName = sizedOldClassName;
                    int endOfString = (index + sizedOldClassName.Length) - editedObjectInfo.DataOffset;
                    while (endOfString % 4 != 0)
                    {
                        endOfString += 1;
                        paddedOldClassName += '\0';
                    }

                    string paddedNewClassName = sizedNewClassName;
                    endOfString = (index + sizedNewClassName.Length) - editedObjectInfo.DataOffset;
                    while (endOfString % 4 != 0)
                    {
                        endOfString += 1;
                        paddedNewClassName += '\0';
                    }

                    bundle.ContentData = bundle.ContentData.Remove(index, paddedOldClassName.Length).Insert(index, paddedNewClassName);
                    int delta = bundle.ContentData.Length - oldSize;

                    Console.WriteLine($"Replaced {oldClassName} to {newClassName} at index {index}, delta {delta}");

                    if (delta != 0)
                    {
                        editedObjectInfo.DataSize += delta;

                        bundle.Header.FileSize += delta;
                        foreach (var objectInfo in bundle.Metadata.Objects)
                        {
                            if (objectInfo.DataOffset > index)
                            {
                                objectInfo.DataOffset += delta;
                            }
                        }
                    }
                }
            }
        }
    }
}
