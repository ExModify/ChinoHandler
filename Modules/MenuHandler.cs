using System;
using System.Collections.Generic;
using ChinoHandler.Models;

namespace ChinoHandler.Modules
{
    public class MenuHandler
    {
        List<MenuOption> menus;
        public MenuHandler()
        {
            menus = new List<MenuOption>();
        }

        public void Add(string Name, Action Action)
        {
            MenuOption option = new MenuOption();
            option.Name = Name;
            option.Action = Action;
            menus.Add(option);
            ReinitalizeNumbers();
        }
        public void Display()
        {
            foreach (MenuOption option in menus)
            {
                Console.WriteLine(option.Number + " - " + option.Name);
            }
        }
        private void ReinitalizeNumbers()
        {
            for (int i = 0; i < menus.Count; i++)
            {
                menus[i].Number = (i + 1).ToString();
            }
        }
        public MenuOption GetOption(string Input)
        {
            foreach (MenuOption option in menus)
            {
                if (option.Name.ToLower() == Input.ToLower() || option.Number == Input)
                {
                    return option;
                }
            }
            return null;
        }
    }
}