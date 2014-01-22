﻿using SourceControl.Build;
using SourceControl.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TaskBroker.Assemblys
{
    public class AssemblyStatus
    {
        public string revision { get; set; }
        public string revisionDate { get; set; }
        public string activeRevision { get; set; }
        public string activeRevisionDate { get; set; }

        public string State { get; set; }

        public AssemblyStatus(SourceControl.Assemblys.AssemblyProject prj)
        {
            State = prj.State.ToString();

            revision = prj.sourceVersionRevision.Revision;
            revisionDate = prj.sourceVersionRevision.CommitTime.ToString();
            activeRevision = prj.edgeStoredVersionRevision.VersionTag;
            activeRevisionDate = "not implemented";
        }
    }

    public class Assemblys
    {
        public SourceControl.Assemblys.AssemblyProjects assemblySources;
        public Assemblys()
        {
            // host packages, modules
            list = new List<AssemblyModule>();
            loadedAssemblys = new Dictionary<string, AssemblyCard>();
            SharedManagedLibraries = new ArtefactsDepot();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // build, update packages: 
            assemblySources = new SourceControl.Assemblys.AssemblyProjects(Directory.GetCurrentDirectory());
        }
        public IEnumerable<KeyValuePair<string, AssemblyStatus>> GetSourceStatuses()
        {
            foreach (SourceControl.Assemblys.AssemblyProject item in assemblySources.hostedProjects)
            {
                yield return new KeyValuePair<string, AssemblyStatus>(item.moduleName, new AssemblyStatus(item));
            }
        }
        public void UpdatePackage(string Name)
        {
            for (int i = 0; i < assemblySources.hostedProjects.Count; i++)
			{
                if (assemblySources.hostedProjects[i].moduleName == Name)
                {
                    assemblySources.hostedProjects[i].SetBuildDeferredFlag();
                    return;
                }
			}
            
        }
        public List<AssemblyModule> list;
        public Dictionary<string, AssemblyCard> loadedAssemblys;

        private ArtefactsDepot SharedManagedLibraries;

        public void AddAssemblySource(string name, string projectRelativePath, string scmUrl)
        {
            assemblySources.Add(name, projectRelativePath, scmUrl);
            AssemblyBinVersions ver = new AssemblyBinVersions(System.IO.Directory.GetCurrentDirectory(), name);
            AssemblyVersionPackage package = ver.GetLatestVersion();
            if (package == null)
            {
                Console.WriteLine("module not well formated, package info not present: {0}", name);
                return;
            }
            list.Add(new AssemblyModule(package));
        }

        public void AddAssembly(string name)
        {
            AssemblyBinVersions ver = new AssemblyBinVersions(System.IO.Directory.GetCurrentDirectory(), name);
            AssemblyVersionPackage package = ver.GetLatestVersion();
            if (package == null)
            {
                Console.WriteLine("module not well formated, package info not present: {0}", name);
                return;
            }
            list.Add(new AssemblyModule(package));
        }
        public void AddAssembly(AssemblyVersionPackage package)
        {
            list.Add(new AssemblyModule(package));
        }
        public void LoadAssemblys(Broker b)
        {
            loadedAssemblys.Clear();
            // in order to reject only new modules -if depconflict persist-
            foreach (AssemblyModule a in list.OrderBy(am => am.package.Version.AddedAt))
            {
                 LoadAssembly(b, a);
            }
        }
        private bool LoadAssembly(Broker b, AssemblyModule a)
        {
            try
            {
                SharedManagedLibraries.RegisterAssets(a.package);
                AddAssemblyUnsafe(b, a);
            }
            catch (Exception e)
            {
                a.RutimeLoadException = string.Format("assembly loading error: '{0}' :: {1}", a.PathName, e.Message);
                Console.WriteLine(a.RutimeLoadException);
                return a.RuntimeLoaded = false;
            }
            return a.RuntimeLoaded = true;
        }

        private void AddAssemblyUnsafe(Broker b, AssemblyModule a)
        {
            Assembly assembly = null;
            if (a.SymbolsPresented)
                assembly = Assembly.Load(a.package.ExtractLibrary(), a.package.ExtractLibrarySymbols());
            else assembly = Assembly.Load(a.package.ExtractLibrary());

            string assemblyName = assembly.GetName().Name;
            AssemblyCard card = new AssemblyCard()
            {
                assembly = assembly,
                AssemblyName = assemblyName
            };
            var type = typeof(IMod);
            var types = assembly.GetTypes().Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

            foreach (Type item in types)
            {
                b.Modules.RegisterInterface(item, assemblyName);
                b.RegisterSelfValuedModule(item);
            }
            card.Interfaces = (from t in types
                               select t.FullName).ToArray();

            loadedAssemblys.Add(assemblyName, card);
        }
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string[] Parts = args.Name.Split(',');
            BuildResultFile asset;
            BuildResultFile assetsym;
            if (SharedManagedLibraries.ResolveLibrary(Parts[0], out asset, out assetsym))
            {
                if (assetsym != null)
                {
                    return Assembly.Load(asset.Data, assetsym.Data);
                }
                else
                {
                    return Assembly.Load(asset.Data);
                }
            }
            else
            {
                Console.WriteLine("loading shared library failed: not found {0}", Parts[0]);
            }
            return null;
        }

    }
}
