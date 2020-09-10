using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using System.Runtime.CompilerServices;
using RimWorld;
using System.Linq;
using System.Collections;
using System.Xml;

namespace PIGEON
{
    [StaticConstructorOnStartup]
    public static class TextureRenamer
    {
        private static readonly string packageId = "PIGEON.Texture.Finder".ToLower();
        private static readonly string steam_path = packageId + "_steam";
        public static List<string> getNodesValue(XmlNode node, string tag, bool append_)
        {
            List<string> result = new List<string>();
            HashSet<string> tmpResult = new HashSet<string>();//使用Hashset防止数据重复
            if (node != null)
            {
                XmlNodeList lis = node.SelectNodes(tag);
                if (lis != null && lis.Count > 0)
                {
                    foreach (XmlNode li in lis)
                    {
                        if (li.InnerText != null && li.InnerText.Length > 0)
                        {
                            if (append_)
                            {
                                tmpResult.Add("_" + (li.InnerText.Trim()));
                            }
                            else
                            {
                                tmpResult.Add(li.InnerText.Trim());
                            }
                        }
                    }
                }
            }
            if (tmpResult.Count > 0)
            {
                foreach(string tmpstr in tmpResult)
                {
                    result.Add(tmpstr);
                }
            }
            return result;

        }
        [CompilerGenerated]
        [Serializable]
        private sealed class PackageFinder
        {
            public static readonly PackageFinder instance = new PackageFinder();
            public static Predicate<ModContentPack> modPack;
            internal bool notNullMod(ModContentPack x)
            {
                return x != null && x.PackageId != null;
            }
        }

        public class XmlReader
        {
            
            public static string findPath()
            {
                List<ModContentPack> before = LoadedModManager.RunningModsListForReading;
                Predicate<ModContentPack> tmpMods;
                if ((tmpMods= PackageFinder.modPack )== null)
                {
                    tmpMods = (PackageFinder.modPack=new Predicate<ModContentPack>(TextureRenamer.PackageFinder.instance.notNullMod));
                }
                List< ModContentPack> modPacks = before.FindAll(tmpMods);
                //Log.Message("modPacks count:" + modPacks.Count);
                
                for (int i = 0; i < modPacks.Count; i++)
                {
                    //Log.Message(modPacks[i].PackageId);
                    //Log.Message(modPacks[i].RootDir);
                    if (packageId == modPacks[i].PackageId.ToLower()|| steam_path== modPacks[i].PackageId.ToLower())
                    {
                        return modPacks[i].RootDir + "/config/config.xml";
                    }
                }
                return "";
            }
            public static List<SettingNode>  readXml()
            {
                List<SettingNode> list = new List<SettingNode>();
                XmlDocument document = new XmlDocument();
                try
                {
                    string path = findPath();
                    Log.Message("load config.xml from :" + path);
                    if (document == null)
                    {
                        Log.Error("can't create instance for xml.");
                        return null;
                    }
                    if (path.Length == 0)
                    {
                        Log.Error("can't read config.xml !");
                        return null;
                    }
                    document.Load(path);//读取xml
                    XmlNode rootNode = document.SelectSingleNode("setting");//读取setting
                    /**---------------------------------*/
                    if (rootNode != null)
                    {
                        List<string> appendGobalList = getNodesValue(rootNode.SelectSingleNode("AppendGobal"), "li",true);
                        List<string> excludeGobalList = getNodesValue(rootNode.SelectSingleNode("ExcludeFoldersGobal"), "li", false);
                        XmlNode packageNodes = rootNode.SelectSingleNode("packages");//获取packages;
                        if (packageNodes != null)
                        {
                            //获取package数组
                            XmlNodeList packageList = packageNodes.SelectNodes("package");
                            if(packageList != null&& packageList.Count > 0)
                            {
                                //判断每个package的有效性
                                foreach (XmlNode package in packageList)
                                {
                                    //获取package的ID
                                    XmlNode packageIdNode= package.SelectSingleNode("packageId");
                                    
                                    string innerText = null;
                                    if (packageIdNode.InnerText != null)
                                    {
                                        innerText = packageIdNode.InnerText.Trim();
                                    }
                                    if (packageIdNode != null&& innerText != null&& innerText.Length>0)
                                    {
                                        SettingNode settingNode = new SettingNode();
                                        settingNode.packagesId = innerText;//设置ID
                                        settingNode.appendPostfix = appendGobalList;//设置追加的数据
                                        List<string> excludes = getNodesValue(package.SelectSingleNode("excludeFolders"), "li",false);
                                        if (excludes.Count > 0)
                                        {
                                            excludes.AddRange(excludeGobalList);
                                           settingNode.excludeFolders = excludes;
                                        }
                                        else
                                        {
                                            settingNode.excludeFolders = excludeGobalList;
                                        }
                                        list.Add(settingNode);
                                    }
                                }//遍历
                            }
                        }
                    }
                  
                }
                catch(Exception e)
                {
                    Log.Error(e.ToString());
                    Log.Error("can't read XML correctly,please check ur xml file.");
                    Log.Error("PIGEON's texture finder won't be load before ur XML set correctly.");
                    return null;//错误，返回失败
                }
                
                return list;
            }
        }
        public class SettingNode
        {
            public string packagesId { get; set; }
            //mod的ID
            public List<string> excludeFolders { get; set; }//遍历该mod的texture应该排除的部分,从头开始遍历

            public List<string> appendPostfix { get; set; }
        }
        public class PathFinder
        {
            private readonly string[] direction = { "_east","_south","_north",""};
            List<SettingNode> setting = null;
            public PathFinder(List<SettingNode> setting)
            {
                this.setting = setting;
            }
            /**
             * 从配置中加载packageId列表
             */
            private List<SettingNode> ReadPackageIdList()
            {
                return setting;
                //new List<SettingNode>();
                //SettingNode test = new SettingNode();
                //test.packagesId = "GloomyLynx.DragonianRace";
                //test.excludeFolders = new List<string>();
                //test.excludeFolders.Add("Hair");
                //test.excludeFolders.Add("Head");

                //test.appendPostfix = new List<string>();
                //test.appendPostfix.Add("_Female");
                //test.appendPostfix.Add("_DD");
                //test.appendPostfix.Add("_Thin");
                //list.Add(test);
                //return list;
            }
            //判断path的前缀是否包括exclude字段，如果包括，则返回true
            private Boolean matches(string path,string exclude)
            {
                return path!=null&&exclude!=null&&path.StartsWith(exclude);
            }
            /**
             * 从mainPath的后面删除excludePostfix
             */
            private string getPrefixPath(string mainPath,string excludePostfix)
            {
                if (excludePostfix != null&& mainPath!=null)
                {
                    int pos = -1;
                    if((pos = mainPath.LastIndexOf(excludePostfix)) > 0)
                    {
                        string result = mainPath.Substring(0, pos);
                        if (result[result.Length - 1] != '/')
                        {
                            result += "/";
                        };//为结尾补充一个 / 
                        return result;
                    }
                }
                return mainPath;
            }
            /**
             * 深度遍历树
             */
            private void deepTree(ModContentPack package,SettingNode node)
            {
                //Log.Message(string.Format("reading packageId:{0} ,node:{1}", package.PackageId, node.excludeFolders.ToString()));
                //获取该mod的所有贴图信息
                Dictionary<string, Texture2D> contentList=package.GetContentHolder<Texture2D>().contentList;
                List<Texture2D> appendToModsList = new List<Texture2D>();
                List<string> appendToModsListName = new List<string>();
                foreach (KeyValuePair<string,Texture2D> kv in contentList)
                {
                    //kv.key :完整的相对路径，从texture之后开始
                    //kv.value.name:文件名（不包括.png）
                    //为假时表示该贴图需要重命名
                    bool flag = false;
                    //判断该mod的贴图是否在exclude中
                    foreach(string exclude in node.excludeFolders)
                    {
                        //Log.Message("load:" + kv.Key);
                        if (matches(kv.Key, exclude))
                        {
                            //Log.Message("skip");
                            flag = true;
                            continue;
                        }
                    }
                    if (!flag)
                    {
                        Texture2D texture = kv.Value;
                        string newPath = getPrefixPath(kv.Key, texture.name);//路径后缀，放在最前
                        string postfix_tag = "";//方向后缀，放在最后
                        string newName = texture.name;//新命名
                        int skipIndex = -1;
                        for(int i = 0; i < direction.Length; i++)
                        {
                            if (newName.EndsWith(direction[i]))
                            {
                                postfix_tag = direction[i];
                                newName= newName.Substring(0, newName.LastIndexOf(direction[i]));
                                break;
                            }
                        }
                        List<string> append = node.appendPostfix;
                        for(int i=0;i< append.Count; i++)
                        {
                            if (newName.EndsWith(append[i]))
                            {
                                skipIndex = i;
                                newName = newName.Substring(0, newName.LastIndexOf(append[i]));
                                break;
                            }
                        }
                        //Log.Message("the newName is :" + newName);
                        for (int i = 0; i < append.Count; i++)
                        {
                            //跳过已经存在的贴图
                            if (skipIndex != i)
                            {
                                string fileAbsoluteName = string.Format("{0}{1}{2}{3}", newPath, newName, append[i], postfix_tag);
                                //Log.Message(string.Format("add {0} to path:{1}", texture.name, fileAbsoluteName));
                                appendToModsListName.Add(fileAbsoluteName);
                                appendToModsList.Add(texture);
                                
                            }
                        }
                    }
                   
                }
                //把需要添加的数据添加到该mod中
                Boolean errorFlag = false;
                for (int i = 0; i < appendToModsList.Count; i++)
                {
                    try
                    {
                        package.GetContentHolder<Texture2D>().contentList.Add(appendToModsListName[i], appendToModsList[i]);
                    }catch(Exception e)
                    {
                        errorFlag = true;//报错提醒
                    }
                }
                if (errorFlag)
                {
                    Log.Warning("an error happened when appending texture ,maybe this texture is exist ,it doesn't affect ur game");
                }
                //结束
            }
            public void PatchTexture()
            {
                List<ModContentPack> modPacks = LoadedModManager.RunningModsListForReading;
                //Log.Message("modPacks:" + modPacks.Count);//LOG
                List<SettingNode> packages= ReadPackageIdList();//获取需要追加贴图的mod
                if(packages!=null&& packages.Count > 0)//有mod才往下执行
                {
                    //modPacks.ForEach(v => Log.Message("finded packagesId:"+v.PackageId));//LOG
                    Dictionary<string, ModContentPack> dict = modPacks.ToDictionary(key => key.PackageId.ToLower(), obj => obj);//将list转换为map
                    for(int i = 0; i < packages.Count; i++)
                    //foreach(SettingNode node in packages)//遍历数组，判断packagesId在mod中是否存在
                    {
                        SettingNode node = packages[i];
                        //Log.Message("search node:" + node.packagesId);//LOG
                        //节点判空，存在对应的packagesId,使用小写判断
                        if (node != null&& node.packagesId!=null&&dict.ContainsKey(node.packagesId.ToLower()))
                        {
                            //如果没有需要为该mod添加的后缀，则跳过
                            if (node.appendPostfix != null && node.appendPostfix.Count > 0)
                            {
                                //深度遍历该节点，替换数据
                                deepTree(dict[node.packagesId.ToLower()], node);
                            }
                           
                        }
                    }
                }
            }

        }
        public static string arryString(List<string> list)
        {
            if (list != null&& list.Count>0)
            {
                string res = "[";
                foreach(string str in list)
                {
                    res += str+" , ";
                }
                res += "]";
                return res;
            }
            return "[]";
        }
        static TextureRenamer()
        {
            //Log.Message("loading...pathfinder");
            List<SettingNode> setting = XmlReader.readXml();
            if (setting == null || setting.Count == 0)
            {
                Log.Warning("can't read data from PIGEON's texture finder /config.xml");
                Log.Warning("PIGEON's texture finder won't be load before ur XML set correctly.");
                return;
            }
            
            Log.Message("-----------------PIGEON's texture finder start----------------");
            Log.Message(string.Format("append postfix:{0}", setting[0].appendPostfix));
            foreach (SettingNode node in setting)
            {
                Log.Message(string.Format("search packageId:{0},excludeFolders:{1}", node.packagesId, arryString(node.excludeFolders)));
            }
            PathFinder finder = new PathFinder(setting);
            finder.PatchTexture();
            Log.Message("-----------------PIGEON's texture finder end----------------");
            
        }
    }
}
