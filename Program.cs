using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;

namespace csharp_to_dts
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = Path.GetFileName(args[0]);
            var path = Path.GetDirectoryName(args[0]);

            var ass = Assembly.LoadFile(args[0]);
            var referAss = ass.GetReferencedAssemblies();

            foreach (var refer in referAss)
            {
                try {
                    Assembly.Load(refer);
                } catch {
                    Assembly.LoadFrom(path + "\\" + refer.Name + ".dll");
                }
            }

            var inter = ass.GetTypes()
                .Where(
                    x => x.IsInterface 
                    || x.Name == "ActionPlanItem" 
                    || x.Name == "ActionPlan" 
                    || x.Name == "TaskReminderDetails" 
                    || x.Name == "SharedoProvider"
                    || x.Name == "DocumentBuilder"
                    || x.Name == "ShareDoActions");

            var output = new StringBuilder();
            foreach (var i in inter)
            {
                output.AppendLine(ExtractData(i));
            }

            File.WriteAllText($".\\{file}.d.ts", output.ToString());

        }

        private static string ExtractData(Type inter)
        {
            var typings = new StringBuilder();
            var type = inter.IsInterface ? "interface" : "class";        

            typings.AppendLine($"declare {type} " + inter.ToJs() + " { ");

            var props = inter.GetProperties();
            var methods = inter.GetMethods();
            foreach (var p in props)
                typings.AppendLine("\t" + p.Name + ": " + p.GetMethod.ReturnType.ToJs());

            foreach (var m in methods)
                if(!m.IsSpecialName)
                    typings.AppendLine("\t" + m.Name + "(" +  String.Concat(m.GetParameters().SelectMany((i,j) => (j > 0 ? ", " : "") + i.Name + ": " + i.ParameterType.ToJs() )) +") : " + m.ReturnType.ToJs());                

            typings.AppendLine("}");
            
            return typings.ToString();     
        }       
    }

    public static class JsHelper {
        public static string ToJs(this Type t)
        {
            if (t.IsGenericType)
            {
                var genericArgs = string.Concat(t.GetGenericArguments().SelectMany((x, y) => (y > 0 ? "," : "") + x.ToJs()));
                return MapTypeName(t).Replace("`", "$") + "<" + genericArgs + ">";
            }

            return MapTypeName(t);

        }

        private static string MapTypeName(Type t)
        {
            switch (t.Name)
            {
                case "String":
                    return "string";
                case "String[]":
                    return "string[]";
                case "Void":
                    return "void";
                case "Int32":
                    return "number";
                case "Boolean":
                    return "Boolean";
                case "Dictionary`2":
                    return "System.Collections.Generic.IDictionary$2";
                case "List`1":
                    return "System.Collections.Generic.IList$1";
                default:
                    return (t.IsClass || t.IsInterface) && t.Assembly.FullName.StartsWith("EventEngine")
                    ? t.Name
                    : t.Namespace + "." + t.Name;
            }
        }
    }
}
