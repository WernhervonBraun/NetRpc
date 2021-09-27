﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using NetRpc.Contract;

namespace NetRpc
{
    public sealed class ContractMethod
    {
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// Func/Cancel will map to _conn_id, _callId
        /// </summary>
        public ReadOnlyCollection<PPInfo> InnerSystemTypeParameters { get; }

        public ContractMethod(Type contractType, List<SwaggerRoleAttribute> contractTypeRoles, string contractTypeTag, MethodInfo methodInfo,
            List<FaultExceptionAttribute> faultExceptionAttributes, List<HttpHeaderAttribute> httpHeaderAttributes,
            List<ResponseTextAttribute> responseTextAttributes, List<SecurityApiKeyAttribute> securityApiKeyAttributes)
        {
            MethodInfo = methodInfo;
            InnerSystemTypeParameters = new ReadOnlyCollection<PPInfo>(InnerType.GetInnerSystemTypeParameters(methodInfo));
            FaultExceptionAttributes = new ReadOnlyCollection<FaultExceptionAttribute>(faultExceptionAttributes);
            HttpHeaderAttributes = new ReadOnlyCollection<HttpHeaderAttribute>(httpHeaderAttributes);
            ResponseTextAttributes = new ReadOnlyCollection<ResponseTextAttribute>(responseTextAttributes);
            SecurityApiKeyAttributes = new ReadOnlyCollection<SecurityApiKeyAttribute>(securityApiKeyAttributes);

            //IgnoreAttribute
            IsGrpcIgnore = GetCustomAttribute<GrpcIgnoreAttribute>(contractType, methodInfo) != null;
            IsRabbitMQIgnore = GetCustomAttribute<RabbitMQIgnoreAttribute>(contractType, methodInfo) != null;
            IsHttpIgnore = GetCustomAttribute<HttpIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerIgnore = GetCustomAttribute<TracerIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerArgsIgnore = GetCustomAttribute<TracerArgsIgnoreAttribute>(contractType, methodInfo) != null;
            IsTraceReturnIgnore = GetCustomAttribute<TracerReturnIgnoreAttribute>(contractType, methodInfo) != null;
            IsImages = GetCustomAttribute<HttpImagesAttribute>(contractType, methodInfo) != null;

            Route = new MethodRoute(contractType, methodInfo);
            var mqPostAttribute = GetCustomAttribute<MQPostAttribute>(contractType, methodInfo);
            IsMQPost = mqPostAttribute != null;
            MqPriority = mqPostAttribute?.Priority ?? 0;
            IsHideFaultExceptionDescription = GetCustomAttribute<HideFaultExceptionDescriptionAttribute>(contractType, methodInfo) != null;
            Tags = new ReadOnlyCollection<string>(GetTags(contractTypeTag, methodInfo));

            //SwaggerRole
            var roles = GetRoles(contractTypeRoles, methodInfo);
            Roles = new ReadOnlyCollection<string>(roles);
        }

        /// <summary>
        /// if null means all contract methods roles is unset.
        /// </summary>
        public ReadOnlyCollection<string>? Roles { get; }

        public ReadOnlyCollection<string> Tags { get; }

        public MethodRoute Route { get; }

        public bool IsTracerArgsIgnore { get; }

        public bool IsTraceReturnIgnore { get; }

        public bool IsGrpcIgnore { get; }

        public bool IsRabbitMQIgnore { get; }

        public bool IsHttpIgnore { get; }

        public bool IsTracerIgnore { get; }

        public bool IsMQPost { get; }

        public bool IsImages { get; }

        /// <summary>
        /// 队列优先级
        /// </summary>
        public byte MqPriority { get; set; }

        public bool IsHideFaultExceptionDescription { get; }

        public bool InRoles(IList<string> roles)
        {
            if (Roles == null)
                return true;

            foreach (var role in roles)
            {
                if (Roles.Any(i => i == role))
                    return true;
            }

            return false;
        }

        public ReadOnlyCollection<FaultExceptionAttribute> FaultExceptionAttributes { get; }

        public ReadOnlyCollection<HttpHeaderAttribute> HttpHeaderAttributes { get; }

        public ReadOnlyCollection<ResponseTextAttribute> ResponseTextAttributes { get; }

        public ReadOnlyCollection<SecurityApiKeyAttribute> SecurityApiKeyAttributes { get; }

        public object? CreateMergeArgTypeObj(string? callId, string? connectionId, long streamLength, object?[] args)
        {
            if (Route.DefaultRout.MergeArgType.Type == null)
                return null;

            args = InnerType.GetInnerPureArgs(args, Route.DefaultRout);

            var instance = Activator.CreateInstance(Route.DefaultRout.MergeArgType.Type);
            var newArgs = args.ToList();

            var i = 0;
            foreach (var p in Route.DefaultRout.MergeArgType.Type.GetProperties())
            {
                switch (p.Name)
                {
                    case CallConst.ConnIdName:
                        p.SetValue(instance, connectionId);
                        break;
                    case CallConst.CallIdName:
                        p.SetValue(instance, callId);
                        break;
                    case CallConst.StreamLength:
                        p.SetValue(instance, streamLength);
                        break;
                    default:
                        p.SetValue(instance, newArgs[i]);
                        break;
                }

                i++;
            }

            return instance;
        }

        private static T? GetCustomAttribute<T>(Type contractType, MethodInfo methodInfo) where T : Attribute
        {
            var methodA = methodInfo.GetCustomAttribute<T>(true);
            if (methodA != null)
                return methodA;

            return contractType.GetCustomAttribute<T>(true);
        }

        private static List<string> GetTags(string contractTypeTag, MethodInfo methodInfo)
        {
            var ret = new List<string>();
            ret.Add(contractTypeTag);

            var tags = methodInfo.GetCustomAttributes<TagAttribute>(true);
            foreach (var t in tags)
                ret.Add(t.Name);

            return ret;
        }

        private static List<string> GetRoles(List<SwaggerRoleAttribute> instanceRoleAttributes, MethodInfo methodInfo)
        {
            var roles = new List<string>();
            var notRoles = new List<string>();

            var instanceRoleAttributes2 = instanceRoleAttributes.ToList();
            instanceRoleAttributes2.AddRange(methodInfo.GetCustomAttributes<SwaggerRoleAttribute>(true));

            foreach (var attr in instanceRoleAttributes2)
            {
                var r = Parse(attr.Role);
                roles.AddRange(r.roles);
                notRoles.AddRange(r.notRoles);
            }

            roles = roles.Distinct().ToList();
            notRoles = notRoles.Distinct().ToList();

            var ret = new List<string>();
            foreach (var role in roles)
            {
                if (!notRoles.Exists(i => i == role))
                    ret.Add(role);
            }

            return ret;
        }

        private static (List<string> roles, List<string> notRoles) Parse(string? s)
        {
            List<string> roles = new();
            List<string> notRoles = new();

            if (s == null)
                return (roles, notRoles);

            s = s.Trim().ToLower();
            if (s == "")
                return (roles, notRoles);

            var ss = s.Split(',');
            foreach (var s1 in ss)
            {
                var s2 = s1.Trim();
                if (s2 == "")
                    continue;

                if (s2.StartsWith("!"))
                {
                    s2 = s2.Substring(1);
                    if (s2 == "")
                        continue;
                    notRoles.Add(s2);
                }
                else
                    roles.Add(s1);
            }

            return (roles, notRoles);
        }
    }

    public sealed class ContractInfo
    {
        /// <param name="contractType"></param>
        /// <param name="instanceType">Set instanceType for get SwaggerRole attributes, in http channel service side.</param>
        public ContractInfo(Type contractType, Type? instanceType = null)
        {
            Type = contractType;

            SecurityApiKeyDefineAttributes = new ReadOnlyCollection<SecurityApiKeyDefineAttribute>(
                Type.GetCustomAttributes<SecurityApiKeyDefineAttribute>(true).ToList());

            var methodInfos = Type.GetInterfaceMethods().ToList();
            var tag = GetTag(Type);
            var contractTypeRoles = GetRoleAttributes(contractType);

            //faultDic
            var faultDic = GetFaultDic(contractType, methodInfos);

            //otherDic
            var apiKeysDic = GetItemsFromDefines<SecurityApiKeyAttribute, SecurityApiKeyDefineAttribute>(Type, methodInfos,
                (i, define) => i.Key == define.Key);
            var httpHeaderDic = GetAttributes<HttpHeaderAttribute>(Type, methodInfos);
            var responseTextDic = GetAttributes<ResponseTextAttribute>(Type, methodInfos);

            //methods
            var methods = new List<ContractMethod>();
            foreach (var (key, value) in faultDic)
                methods.Add(new ContractMethod(
                    Type,
                    contractTypeRoles,
                    tag,
                    key,
                    value,
                    httpHeaderDic[key],
                    responseTextDic[key],
                    apiKeysDic[key]));

            Methods = new ReadOnlyCollection<ContractMethod>(methods);
            Tags = new ReadOnlyCollection<string>(GetTags(methods));
        }

        public Type Type { get; }

        public ReadOnlyCollection<SecurityApiKeyDefineAttribute> SecurityApiKeyDefineAttributes { get; }

        public ReadOnlyCollection<ContractMethod> Methods { get; }

        public ReadOnlyCollection<string> Tags { get; }

        public ReadOnlyCollection<ContractMethod> GetMethods(IList<string> roles)
        {
            List<ContractMethod> ret = new();
            foreach (var m in Methods)
                if (m.InRoles(roles))
                    ret.Add(m);
            return new ReadOnlyCollection<ContractMethod>(ret);
        }

        private Dictionary<MethodInfo, List<FaultExceptionAttribute>> GetFaultDic(Type contractType, List<MethodInfo> methodInfos)
        {
            var isInheritedFault = contractType.GetCustomAttribute<InheritedFaultExceptionDefineAttribute>() != null;
            var existFaultExceptionDefines = GetFaultExceptionDefineFromGroup(contractType);
            var faultDic = GetItemsFromDefines(
                Type,
                methodInfos,
                (i, define) => i.DetailType == define.DetailType,
                i => new FaultExceptionAttribute(i.DetailType, i.StatusCode, i.ErrorCode, i.Description),
                existFaultExceptionDefines,
                isInheritedFault);
            return faultDic;
        }

        private static List<FaultExceptionDefineAttribute> GetFaultExceptionDefineFromGroup(Type contractType)
        {
            List<FaultExceptionDefineAttribute> ret = new();
            foreach (var a in contractType.GetCustomAttributes(true))
            {
                if (a is IFaultExceptionGroup feg)
                    ret.AddRange(feg.FaultExceptionDefineAttributes);
            }

            return ret;
        }

        private static Dictionary<MethodInfo, List<T>> GetItemsFromDefines<T, TDefine>(
            Type contractType,
            IEnumerable<MethodInfo> methodInfos,
            Func<T, TDefine, bool> match)
            where T : Attribute
            where TDefine : Attribute
        {
            var dic = new Dictionary<MethodInfo, List<T>>();
            var defines = contractType.GetCustomAttributes<TDefine>(true).ToList();

            var items = contractType.GetCustomAttributes<T>(true).ToList();

            foreach (var m in methodInfos)
            {
                var tempItems = m.GetCustomAttributes<T>(true).ToList();
                tempItems.AddRange(items);
                foreach (var f in tempItems)
                {
                    var foundF = defines.FirstOrDefault(i => match(f, i));
                    if (foundF != null)
                        f.CopyPropertiesFrom(foundF);
                }

                dic[m] = tempItems;
            }

            return dic;
        }

        private static Dictionary<MethodInfo, List<T>> GetItemsFromDefines<T, TDefine>(
            Type contractType,
            IEnumerable<MethodInfo> methodInfos,
            Func<T, TDefine, bool> match,
            Func<TDefine, T> convert,
            List<TDefine> existDefines,
            bool isInheritedDefines)
            where T : Attribute
            where TDefine : Attribute
        {
            var dic = new Dictionary<MethodInfo, List<T>>();
            var defines = contractType.GetCustomAttributes<TDefine>(true).ToList();
            defines.AddRange(existDefines);

            var items = contractType.GetCustomAttributes<T>(true).ToList();

            if (isInheritedDefines)
                items.AddRange(existDefines.ConvertAll(i => convert(i)));

            foreach (var m in methodInfos)
            {
                var tempItems = m.GetCustomAttributes<T>(true).ToList();
                tempItems.AddRange(items);
                foreach (var f in tempItems)
                {
                    var foundF = defines.FirstOrDefault(i => match(f, i));
                    if (foundF != null)
                        f.CopyPropertiesFrom(foundF);
                }

                dic[m] = tempItems;
            }

            return dic;
        }

        private static Dictionary<MethodInfo, List<T>> GetAttributes<T>(Type type, IEnumerable<MethodInfo> methodInfos) where T : Attribute
        {
            var dic = new Dictionary<MethodInfo, List<T>>();
            var typeAttrs = type.GetCustomAttributes<T>(true).ToList();
            foreach (var m in methodInfos)
            {
                var tempL = typeAttrs.ToList();
                tempL.AddRange(m.GetCustomAttributes<T>(true).ToList());
                dic[m] = tempL;
            }

            return dic;
        }

        private static string GetTag(Type type)
        {
            var tags = type.GetCustomAttributes<TagAttribute>(true).ToList();
            if (tags.Count > 1)
                throw new InvalidOperationException("TagAttribute on Interface is not allow multiple.");

            if (tags.Count == 0)
                return type.Name;
            return tags[0].Name;
        }

        private static List<string> GetTags(List<ContractMethod> methods)
        {
            var ret = new List<string>();
            methods.ForEach(i => ret.AddRange(i.Tags));
            return ret.Distinct().ToList();
        }

        private static List<SwaggerRoleAttribute> GetRoleAttributes(Type contractType)
        {
            var ret = contractType.GetCustomAttributes<SwaggerRoleAttribute>(true).ToList();
            if (!ret.Exists(i => i.Role == "default"))
                ret.Add(new SwaggerRoleAttribute("default"));
            return ret;
        }
    }
}