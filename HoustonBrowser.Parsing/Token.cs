﻿using System;
using System.Collections.Generic;
using System.Text;
using HoustonBrowser.Parsing.Enums;

namespace HoustonBrowser.Parsing
{
    public struct Token
    {
        int type;
        string value;

        public int Type { get => type; set => type=value; }
        public string Value { get => value; set => this.value=value; }

        public Token(int type,string value)
        {
            this.type=type;
            this.value =value;
        }
        //public Token()
        //{
        //    type=(int)TokenType.Null;
        //    value="";
        //}
        public void Standartize()
        {
            value = value.ToLower();
        }
    }
}
