﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using ManyConsole;

//namespace Raft.Commands.ArgumentTypes
//{
//    public abstract class ArgumentPair : IArgument
//    {
//        public string Command { get; set; }
//        public string Description { get; set; }
//        public bool Required { get; set; }
//        public bool Supplied { get; set; }
//        public bool Valid { get; set; }

//        public ArgumentPair(string command, string desc, bool required = false)
//        {
//            Command = command;
//            Description = desc;
//            Required = required;
//        }

//        public void Parse(string s1, string s2)
//        {
//            Supplied = true;
//            Valid = parse(s1, s2);
//        }

//        protected abstract bool parse(string s1, string s2);

//        public void Register(ConsoleCommand cmd)
//        {
//            if (Required)
//                cmd.HasRequiredOption(Command, Description, Parse);
//            else
//                cmd.HasOption(Command, Description, Parse);
            
//        }
//    }
//}
