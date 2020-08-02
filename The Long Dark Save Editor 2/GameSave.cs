using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using The_Long_Dark_Save_Editor_2.Game_data;
using The_Long_Dark_Save_Editor_2.Helpers;
using The_Long_Dark_Save_Editor_2.Serialization;

namespace The_Long_Dark_Save_Editor_2
{
    public class GameSave
    {
        public static int MAX_BACKUPS = 20;

        public long LastSaved { get; set; }
        private DynamicSerializable<BootSaveGameFormat> dynamicBoot;
        public BootSaveGameFormat Boot { get { return dynamicBoot.Obj; } }

        private DynamicSerializable<GlobalSaveGameFormat> dynamicGlobal;
        public GlobalSaveGameFormat Global { get { return dynamicGlobal.Obj; } }

        public AfflictionsContainer Afflictions { get; set; }

        private DynamicSerializable<SlotData> dynamicSlotData;
        public SlotData SlotData { get { return dynamicSlotData.Obj; } }

        public string OriginalRegion { get; set; }

        public string path;

        public void LoadSave(string path)
        {
            this.path = path;
            string slotJson = EncryptString.Decompress(File.ReadAllBytes(path));
            dynamicSlotData = new DynamicSerializable<SlotData>(slotJson);

            var bootJson = EncryptString.Decompress(SlotData.m_Dict["boot"]);
            dynamicBoot = new DynamicSerializable<BootSaveGameFormat>(bootJson);
            OriginalRegion = Boot.m_SceneName.Value;

            var globalJson = EncryptString.Decompress(SlotData.m_Dict["global"]);
            dynamicGlobal = new DynamicSerializable<GlobalSaveGameFormat>(globalJson);

            Afflictions = new AfflictionsContainer(Global);
        }

        public void Save()
        {
            Backup();

            LastSaved = DateTime.Now.Ticks;
            var bootSerialized = dynamicBoot.Serialize();
            SlotData.m_Dict["boot"] = EncryptString.Compress(bootSerialized);

            if (Boot.m_SceneName.Value != OriginalRegion)
            {
                Global.GameManagerData.SceneTransition.m_ForceNextSceneLoadTriggerScene = null;
            }
            Global.GameManagerData.SceneTransition.m_SceneSaveFilenameCurrent = Boot.m_SceneName.Value;
            Global.GameManagerData.SceneTransition.m_SceneSaveFilenameNextLoad = Boot.m_SceneName.Value;
            Global.PlayerManager.m_CheatsUsed = false;
            Afflictions.SerializeTo(Global);

            var globalSerialized = dynamicGlobal.Serialize();
            SlotData.m_Dict["global"] = EncryptString.Compress(globalSerialized);

            SlotData.m_Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var slotDataSerialized = dynamicSlotData.Serialize();
            File.WriteAllBytes(this.path, EncryptString.Compress(slotDataSerialized));
        }

        private static JsonLoadSettings JsonSettLoad = new JsonLoadSettings() { LineInfoHandling = LineInfoHandling.Ignore, CommentHandling = CommentHandling.Ignore };
        private static JsonSerializerSettings JsonSettSerializer = new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Include, TypeNameHandling = TypeNameHandling.None };

        public void JSONLoadSave(bool DeserializeGlobal=false)
        {
            var json = EncryptString.Decompress(File.ReadAllBytes(path));
            Directory.CreateDirectory(path+".json");
            File.WriteAllText(path+".json\\"+Path.GetFileName(path)+".json", json);

            DynamicSerializable<SlotData> fileSerializable = new DynamicSerializable<SlotData>(json);

            foreach (System.Collections.Generic.KeyValuePair<string, byte[]> entry in fileSerializable.Obj.m_Dict)
            {
                var dictJson = EncryptString.Decompress(fileSerializable.Obj.m_Dict[entry.Key]);
                File.WriteAllText(path+".json\\"+Path.GetFileName(path)+"."+entry.Key+".json", dictJson);


                if (entry.Key == "global")
                {
                    if (DeserializeGlobal)
                    {
                        Directory.CreateDirectory(path+".json\\global");
                        JObject global = JObject.Parse(dictJson, JsonSettLoad);
                        foreach (System.Collections.Generic.KeyValuePair<string, JToken> entry2 in global)
                        {
                            string path2_prep = path+".json\\global\\"+entry2.Key+".json";

                            // invalid json: unquoted keys
                            if (entry2.Key == "m_PanelStatsSerialized")
                                continue;

                            if (entry2.Value.Type == JTokenType.Null)
                                File.WriteAllText(path2_prep, "null");
                            else if (entry2.Value.Type == JTokenType.Boolean)
                                if ((bool)JsonConvert.DeserializeObject(entry2.Value.ToString(Formatting.None), JsonSettSerializer))
                                    File.WriteAllText(path2_prep, "true");
                                else
                                    File.WriteAllText(path2_prep, "false");
                            else
                                File.WriteAllText(path2_prep, JsonConvert.DeserializeObject(entry2.Value.ToString(Formatting.None), JsonSettSerializer).ToString());
                        }
                    }
                    else if (Directory.Exists(".json\\global"))
                        Directory.Delete(path+".json\\global", true);

                }
            }

        }

        public void JSONSave()
        {
            Backup();

            var json = File.ReadAllText(path+".json\\"+Path.GetFileName(path)+".json");
            DynamicSerializable<SlotData> fileSerializable = new DynamicSerializable<SlotData>(json);
            
            foreach (string path2 in Directory.GetFiles(Path.GetDirectoryName(path+".json\\"+Path.GetFileName(path)), Path.GetFileName(path)+".*.json")) {
                var regex = new Regex(Regex.Escape(Path.GetFileName(path)));
                var suffix = regex.Replace(Path.GetFileName(path2), "", 1);
                suffix = suffix.Substring(1, suffix.Length-6);

                var dict = File.ReadAllText(path+".json\\"+Path.GetFileName(path)+"."+suffix+".json");
                var dictSerializable = JObject.Parse(dict, JsonSettLoad);

                // DeserializeGlobal
                if (suffix == "global" && Directory.Exists(path+".json\\global"))
                {
                    foreach (string path3 in Directory.GetFiles(Path.GetDirectoryName(path+".json\\global\\"), "*.json"))
                    {
                        string key = Path.GetFileName(path3);
                        key = key.Substring(0, key.Length-5);
                        string tmp = File.ReadAllText(path3);

                        // no need to serialize non-objects
                        JToken val = Regex.IsMatch(tmp, @"^\s*({|\[)(.|\n)*(}|\])\s*$", RegexOptions.Multiline)
                                     ? JToken.Parse(JsonConvert.SerializeObject(tmp, Formatting.None), JsonSettLoad)
                                     : JToken.Parse(tmp, JsonSettLoad);

                        if (!dictSerializable.ContainsKey(key))
                            dictSerializable.Add(key, val);
                        else
                            dictSerializable[key].Replace(val);
                    }
                }
                // /DeserializeGlobal

                fileSerializable.Obj.m_Dict[suffix] = EncryptString.Compress(JsonConvert.SerializeObject(dictSerializable, JsonSettSerializer));
            }

            File.WriteAllBytes(this.path, EncryptString.Compress(fileSerializable.Serialize()));
        }

        private void Backup()
        {
            var backupDirectory = Path.Combine(Path.GetDirectoryName(this.path), "backups");
            Directory.CreateDirectory(backupDirectory);

            var oldBackups = new DirectoryInfo(backupDirectory).GetFiles().OrderByDescending(x => x.LastWriteTime).Skip(MAX_BACKUPS);
            foreach (var file in oldBackups)
            {
                File.Delete(file.FullName);
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture);
            var i = 1;
            var backupPath = Path.Combine(backupDirectory, timestamp + "-" + Path.GetFileName(this.path) + ".backup");
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(backupDirectory, timestamp + "-" + Path.GetFileName(this.path) + "(" + i++ + ")" + ".backup");
            }
            File.Copy(this.path, backupPath);
        }
    }
}
