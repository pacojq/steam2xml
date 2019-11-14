using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace steam2xml
{
    /// <summary>
    /// Paco Juan Quirós. 2019.
    /// </summary>
    internal class Program
    {
        
        public static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Need an input and an output file!");
                Console.WriteLine("Example: ./steam2xml loc_eng.vdf loc_eng.xml");
                return 1;
            }

            string extIn = Path.GetExtension(args[0]);
            string extOut = Path.GetExtension(args[1]);

            if (extIn == ".xml" && extOut == ".vdf")
                return XmlToVdf(args[0], args[1]);
            
            if (extIn == ".vdf" && extOut == ".xml")
                return VdfToXml(args[0], args[1]);
            
            Console.WriteLine("Conversions only happen between XML and VDF files!");
            return 1;
        }

        private class Achievement
        {
            public string Name;
            public string Description;
        }
        
        


        private static int XmlToVdf(string xmlFile, string vdfFile)
        {
            try
            {
                Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
                string language = null;
                string achId = "";
                
                // Read the XML and get the achievements
                
                XmlReader xml = XmlReader.Create(xmlFile);
                while (xml.Read())
                {
                    if (xml.NodeType != XmlNodeType.Element)
                        continue;
                    
                    if (language == null)
                    {
                        xml.MoveToAttribute("language");
                        language = xml.Value;
                    }
                    else
                    {
                        if (xml.HasAttributes)
                        {
                            xml.MoveToAttribute("key");
                            achId = xml.Value;
                            achievements.Add(achId, new Achievement());
                        }
                        else
                        {
                            string name = xml.Name;
                            string value = xml.ReadString();
                            
                            if (name == "name")
                                achievements[achId].Name = value;
                            else achievements[achId].Description = value;
                        }
                    }
                }
                
                
                // Now write everything in the VDF

                VProperty root = new VProperty();
                root.Key = "lang";
                
                VObject obj = new VObject();
                obj.Add("Language", new VValue(language));
                
                VObject tokens = new VObject();
                foreach (var ach in achievements)
                {
                    tokens.Add(ach.Key + "_NAME", new VValue(ach.Value.Name));
                    tokens.Add(ach.Key + "_DESC", new VValue(ach.Value.Description));
                }
                obj.Add("Tokens", tokens);

                VProperty propTokens = new VProperty()
                {
                    Key = "Tokens",
                    Value = new VValue(tokens)
                };

                root.Value = obj;
                
                string result = VdfConvert.Serialize(root);
                
                File.WriteAllText(vdfFile, result);
                
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }
        
        
        
        
        
        
        
        
        private static int VdfToXml(string vdfFile, string xmlFile)
        {
            try
            {
                dynamic vdf = VdfConvert.Deserialize(File.ReadAllText(vdfFile));
                string language = vdf.Value.Language.Value;
                VObject tokens = vdf.Value.Tokens;

                
                // Read all achievements in the file
                
                Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
                foreach (var token in tokens)
                {
                    string key = token.Key;
                    if (key.EndsWith("_NAME"))
                    {
                        key = key.Substring(0, key.Length - 5);
                        if (!achievements.ContainsKey(key))
                            achievements.Add(key, new Achievement());

                        achievements[key].Name = $"{token.Value}";
                    }
                    else if (key.EndsWith("_DESC"))
                    {
                        key = key.Substring(0, key.Length - 5);
                        if (!achievements.ContainsKey(key))
                            achievements.Add(key, new Achievement());

                        achievements[key].Description = $"{token.Value}";
                    }
                }
                
                
                // And now build an XML file with them
                
                using(XmlWriter writer = XmlWriter.Create(
                        new FileStream(xmlFile, FileMode.Create)))
                {
                    writer.WriteStartElement("achievements");
                    writer.WriteAttributeString("language", language);
                    
                    foreach (var pair in achievements)
                    {
                        writer.WriteStartElement("achievement");
                        writer.WriteAttributeString("key", pair.Key);
                        
                        writer.WriteElementString("name", pair.Value.Name);
                        writer.WriteElementString("description", pair.Value.Description);
                        
                        writer.WriteEndElement();
                    }
                    
                    writer.WriteEndElement();
                    writer.Flush();
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }
    }
}