using System.Collections.Specialized;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using ChinoHandler.Models;

namespace ChinoHandler.Modules
{
    public class ConfigEditor
    {
        List<string> NotClass { get => Program.NotClass; }
        public ConfigEditor()
        {
            
        }
        public void FillEmpty(Config Config)
        {
            List<Tuple<PropertyInfo[], object, string>> properties = GetEmptyFields(Config);
            Edit(properties, Config);
        }
        public void EditAll(Config Config)
        {
            List<Tuple<PropertyInfo[], object, string>> properties = GetFields(Config);
            Edit(properties, Config);
        }
        private List<Tuple<PropertyInfo[], object, string>> GetFields(object Object, string Dash = "-")
        {
            List<Tuple<PropertyInfo[], object, string>> properties = new List<Tuple<PropertyInfo[], object, string>>();

            PropertyInfo[] fields = Object.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            properties.Add(new Tuple<PropertyInfo[], object, string>(fields, Object, Dash));

            foreach (PropertyInfo info in fields)
            {
                if (!NotClass.Contains(info.PropertyType.Name))
                {
                    object obj = info.GetValue(Object);
                    properties.AddRange(GetFields(obj, Dash + "-"));
                }
            }

            return properties;
        }
        private List<Tuple<PropertyInfo[], object, string>> GetEmptyFields(object Object)
        {
            List<Tuple<PropertyInfo[], object, string>> properties = GetFields(Object);
            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo[] infos = properties[i].Item1.Where(t => t.GetValue(properties[i].Item2) == null).ToArray();
                if (infos.Length == 0)
                {
                    properties.RemoveAt(i);
                    i--;
                }
                else
                {
                    properties[i] = new Tuple<PropertyInfo[], object, string>(infos, properties[i].Item2, properties[i].Item3);
                }
            }


            return properties;
        }
        private void Edit(List<Tuple<PropertyInfo[], object, string>> Properties, Config Config)
        {
            foreach (Tuple<PropertyInfo[], object, string> tuple in Properties)
            {
                foreach (PropertyInfo property in tuple.Item1)
                {
                    if (property.PropertyType.Name == "String")
                    {
                        EditString(property, tuple.Item2, Config, tuple.Item3);
                    }
                    else if (property.PropertyType.Name == "Int32")
                    {
                        EditInt(property, tuple.Item2, Config, tuple.Item3);
                    }
                }
            }
        }
        private void EditString(PropertyInfo Property, object Object, Config Config, string Dashes)
        {
            string baseValue = (Property.GetValue(Object) ?? "").ToString();
            Console.Write("{0}{1} (Current value: {2})=", Dashes, Property.Name, baseValue);
            string newValue = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                Property.SetValue(Object, newValue);
                Config.SaveConfig();
            }
        }
        private void EditInt(PropertyInfo Property, object Object, Config Config, string Dashes)
        {
            string baseValue = (Property.GetValue(Object) ?? "").ToString();
            Console.Write("{0}{1} (Current value: {2})=", Dashes, Property.Name, baseValue);
            if (int.TryParse(Console.ReadLine(), out int value))
            {
                Property.SetValue(Object, value);
                Config.SaveConfig();
            }
        }
    }
}