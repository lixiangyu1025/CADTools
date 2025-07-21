using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SetupLibrary
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        private string projectName = "CADConverter";

        public Installer()
        {
            InitializeComponent();

            //安装后事件
            AfterInstall += Installer1_AfterInstall;

            //卸载后事件，卸载后会自动删除安装文件夹
            AfterUninstall += Installer1_AfterUninstall;
        }


        private void Installer1_AfterInstall(object sender, InstallEventArgs e)
        {
            //安装后，将CAD插件注册到注册表
            string installerPath = Context.Parameters["targetdir"].Remove(Context.Parameters["targetdir"].Length - 2);    //安装目录
            string dllPath = Path.Combine(installerPath, "CADConverter.dll");

            EditCADRegistry(true, projectName, dllPath);

        }

        private void Installer1_AfterUninstall(object sender, InstallEventArgs e)
        {
            //卸载后，将CAD插件从注册表删除
            EditCADRegistry(false, projectName);

            //一般情况下卸载程序会自动删除安装文件夹，但是可能无法删除cui文件，所以遍历安装目录，删除cui文件
            string installerPath = Context.Parameters["targetdir"].Remove(Context.Parameters["targetdir"].Length - 2);    //安装目录
            foreach (var item in Directory.GetFiles(installerPath))
            {
                try
                {
                    File.Delete(item);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 添加/删除 CAD注册表项（用于CAD启动时自动加载Dll）
        /// <para>适用于所有版本的CAD，CAD内核版本：</para>
        /// <para>2007 - R17.0</para>
        /// <para>2008 - R17.1</para>
        /// <para>2009 - R17.2</para>
        /// <para>2010 - R18.0</para>
        /// <para>2011 - R18.1</para>
        /// <para>2012 - R18.3</para>
        /// <para>2013 - R19.0</para>
        /// <para>2014 - R19.1</para>
        /// <para>2016 - R20.1</para>
        /// <para>2018 - R22.0</para>
        /// <para>2019 - R23.0</para>
        /// <para>2020 - R23.1</para>
        /// <para>2021 - R24.0</para>
        /// <para>2022 - R24.1</para>
        /// <para>2023 - R24.2</para>
        /// <para>2024 - R24.3</para>
        /// </summary>
        /// <param name="register">注册表操作：true，注册-用于安装CAD插件；false，删除-用于卸载CAD插件</param>
        /// <param name="projectName">注册表中显示的项目名称</param>
        /// <param name="dllPath">插件Dll路径：注册时必须传入，删除时不需要</param>
        public static bool EditCADRegistry(bool register, string projectName, string dllPath = "")
        {
            //注册表位置  HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\xxx\ACAD-xxxx:804\Applications\
            Microsoft.Win32.RegistryKey registryKey = null;
            try
            {
                if (Environment.Is64BitOperatingSystem)
                    registryKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    registryKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                Microsoft.Win32.RegistryKey sub = registryKey.OpenSubKey(@"SOFTWARE\Autodesk\AutoCAD");        //需要将项目编译为64位，否则为null
                if (sub != null)
                {

                    //适配不同的CAD版本
                    var versionNo_1s = sub.GetSubKeyNames()?.Where(s => s.StartsWith("R"));   //获取本机安装的所有版本CAD
                    if (versionNo_1s?.Count() > 0)
                    {
                        foreach (var versionNo_1 in versionNo_1s)
                        {
                            Microsoft.Win32.RegistryKey subCAD = sub.OpenSubKey(versionNo_1);
                            var versionNo_2s = subCAD.GetSubKeyNames().Where(c => c.StartsWith("ACAD") && (c.EndsWith("804") || c.EndsWith("409"))); //804 中文；409英文

                            if (versionNo_2s?.Count() > 0)
                            {
                                foreach (var item in versionNo_2s)
                                {
                                    Microsoft.Win32.RegistryKey applications = subCAD.OpenSubKey(item).OpenSubKey("Applications", true);
                                    if (applications != null)
                                    {
                                        if (register)
                                        {
                                            Microsoft.Win32.RegistryKey projectKey = applications.CreateSubKey(projectName);
                                            projectKey.SetValue("DESCRIPTION", projectName, RegistryValueKind.String);
                                            projectKey.SetValue("LOADCTRLS", 0x2, RegistryValueKind.DWord);
                                            projectKey.SetValue("LOADER", dllPath, RegistryValueKind.String);
                                            projectKey.SetValue("MANAGED", 0x1, RegistryValueKind.DWord);
                                        }
                                        else
                                        {
                                            if (applications.OpenSubKey(projectName) != null)
                                                applications.DeleteSubKey(projectName);
                                        }
                                    }
                                }
                            }
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(ex.Message);
            }
            finally
            {
                registryKey.Dispose();
            }
        }
    }
}
