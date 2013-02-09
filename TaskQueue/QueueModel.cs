﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TaskQueue
{
    public class QueueItemModel
    {
        public ValueMap<string, TItemValue_Type> schema = new ValueMap<string, TItemValue_Type>();

        //public void MapPropsToModel<T>(ITItem source, T destination)
        //{
        //    foreach (ValueMapItem<string, TItemValue> sk in source.GetValues())
        //    {
        //        PropertyInfo pi = typeof(T).GetProperty(sk.Value1);
        //        if (pi == null)
        //            continue;

        //        pi.SetValue(destination, sk.Value2.Value, null);
        //    }
        //}
        public QueueItemModel(Type classWithProps)
        {
            PropertyInfo[] props = classWithProps.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    bool isnull;
                    TItemValue_Type itv = GetRType(prop.PropertyType, out isnull);
                    schema.Add(prop.Name, itv);
                }
            }
        }

        public static TItemValue_Type GetRType(Type t, out bool nullable)
        {
            nullable = Nullable.GetUnderlyingType(t) != null;
            if (nullable)
                t = Nullable.GetUnderlyingType(t);
            if (t == typeof(int))
            {
                return TItemValue_Type.num_int;
            }
            if (t == typeof(double))
            {
                return TItemValue_Type.num_double;
            }
            if (t == typeof(string))
            {
                return TItemValue_Type.text;
            }
            if (t == typeof(DateTime))
            {
                return TItemValue_Type.datetime;
            }
            if (t == typeof(bool))
            {
                return TItemValue_Type.boolean;
            }
            return TItemValue_Type.text;
        }
    }
}
