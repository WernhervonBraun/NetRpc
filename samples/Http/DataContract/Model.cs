﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataContract
{
    /// <summary>
    /// a CustomObj
    /// </summary>
    public class CustomObj
    {
        /// <summary>
        /// a Name
        /// </summary>
        public NameEnum Name { get; set; }

        /// <summary>
        /// a Date
        /// </summary>
        public DateTime Date { get; set; }

        [DefaultValue("This defalut value of P1")]
        public string P1 { get; set; }

        public InnerObj InnerObj { get; set; } = new InnerObj();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Date)}: {Date}, {nameof(InnerObj)}: {InnerObj}";
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum NameEnum
    {
        John,
        Mary
    }

    public class InnerObj
    {
        public CustomObj CustomObj { get; set; }

        public string P1 { get; set; }

        public override string ToString()
        {
            return $"{nameof(P1)}: {P1}";
        }
    }

    public class CustomCallbackObj
    {
        public int Progress { get; set; }
    }

    public class ComplexStream
    {
        [JsonIgnore]
        public Stream Stream { get; set; }

        public string StreamName { get; set; } //the property will map to file name.

        public InnerObj InnerObj { get; set; }
    }

    /// <summary>
    /// summary of CustomException
    /// </summary>
    //[Serializable]
    public class CustomException : Exception
    {
        public string P1 { get; set; }

        public string P2 { get; set; }

        public CustomException(string message, string p1, string p2) : base(message)
        {
            P1 = p1;
            P2 = p2;
        }

        public CustomException()
        {
        }

        //protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        //{
        //}
    }

    public class CustomException2 : Exception
    {
        public CustomException2()
        {
        }

        protected CustomException2(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}