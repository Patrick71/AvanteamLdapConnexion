using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AvanteamLdapConnexion
{
    public static class Application
    {
        public static void Run()
        {
            var converters = new List<Func<object, string>>() {ConverterBitConverter, ConverterEncodingDefault, ConverterEncodingUtf8, ConverterEncodingUtf32, ConverterEncodingAscii, ConverterEncodingUnicode, ConverterEncodingBigEndianUnicode };
            try
            {
                using (var directoryEntry = new DirectoryEntry())
                {
                    Configuration(directoryEntry);
                    var searcher = CreateSearcher(directoryEntry);
                    foreach (SearchResult result in searcher)
                    {
                        Console.WriteLine("-----------");
                        foreach (var attribut in Config.Attributs)
                        {
                            Console.Write(attribut + "=");
                            WriteAttributValues(attribut, result, converters);
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static void WriteAttributValues(string attribut, SearchResult result, List<Func<object, string>> converter)
        {
            if (attribut == "member")
            {
                var values = ReadAttribut(result, attribut, converter);
                if (string.IsNullOrEmpty(values.FirstOrDefault()))
                {
                    values = ReadAttributeRanged(result, "member").ToList();
                }
                Console.Write(string.Join(", ", values));
            }
            {
                var values = ReadAttribut(result, attribut, converter);
                Console.Write(string.Join(", ", values));

            }
        }

        
        private static string ConverterBitConverter(object rawValue)
        {
            return BitConverter.ToString((byte[])rawValue);
        }

        private static string ConverterEncodingDefault(object rawValue)
        {
            return Encoding.Default.GetString((byte[])rawValue);
        }
        private static string ConverterEncodingUtf8(object rawValue)
        {
            return Encoding.UTF8.GetString((byte[])rawValue);
        }

        private static string ConverterEncodingUtf32(object rawValue)
        {
            return Encoding.UTF32.GetString((byte[])rawValue);
        }

        private static string ConverterEncodingAscii(object rawValue)
        {
            return Encoding.ASCII.GetString((byte[])rawValue);
        }
        private static string ConverterEncodingUnicode(object rawValue)
        {
            return Encoding.Unicode.GetString((byte[])rawValue);
        }

        private static string ConverterEncodingBigEndianUnicode(object rawValue)
        {
            return Encoding.BigEndianUnicode.GetString((byte[])rawValue);
        }

        private static IEnumerable<string> ReadAttributeRanged(SearchResult resource, string attributeName)
        {
            var resourceId = resource.GetDirectoryEntry().NativeGuid;
            using (var de = new DirectoryEntry())
            {
                Configuration(de);

                var ds = new DirectorySearcher(de) { Filter = "(objectGUID=" + ToLdapFilter(resourceId) + ")" };
                return Read<string>(ds, attributeName);
            }
        }
        private static string ToLdapFilter(string resourceId)
        {
            return Regex.Replace(resourceId, @"(.{2})", "\\$1");
        }
        private static List<string> ReadAttribut(SearchResult result, string attribut, List<Func<object, string>> converters)
        {
            if (!result.Properties.Contains(attribut)) return new List<string>();

            //essaie de lire le contenu de l’attribut
            return result.Properties[attribut].Cast<object>()
                .Select(rawValue =>
                {
                    if (rawValue.GetType() == typeof (byte[]))
                    {
                        return "\n\t" + string.Join("\n\t", converters.Select(c => c.GetMethodInfo().Name +":\t"+ c(rawValue)).ToList());
                    }
                    return rawValue.ToString();
                })
                .ToList();
        }



        private const string RangeSeparator = "-";
        private const string RangeKey = ";range=";
        private const string RangedAttributeFormat = "{0}" + RangeKey + "{1}" + RangeSeparator + "{2}";
        private static string GetResultName(SearchResult result, string propertyName)
        {
            var resultName = (from string name in result.Properties.PropertyNames
                              where name.StartsWith(propertyName + RangeKey, StringComparison.OrdinalIgnoreCase)
                              select name).FirstOrDefault();

            if (resultName != null)
                return resultName;

            resultName = (from string name in result.Properties.PropertyNames
                          where string.Compare(name, propertyName, StringComparison.OrdinalIgnoreCase) == 0
                          select name).FirstOrDefault();

            return resultName;
        }

        private static bool ParseRangedPropertyName(string propertyName, out int start, out int end)
        {
            start = 0;
            end = 0;
            var rangeLocation = propertyName.IndexOf(RangeKey, StringComparison.OrdinalIgnoreCase);
            var seperatorLocation = propertyName.IndexOf(RangeSeparator, StringComparison.OrdinalIgnoreCase);

            if (rangeLocation <= 0 || seperatorLocation <= 0
                || !int.TryParse(propertyName.Substring(rangeLocation + RangeKey.Length,
                    seperatorLocation - rangeLocation - RangeKey.Length), out start))
            {
                return false;
            }

            if (propertyName.Substring(seperatorLocation + 1).Equals("*"))
            {
                end = int.MaxValue;
            }

            if (end != int.MaxValue && !int.TryParse(propertyName.Substring(seperatorLocation + 1), out end))
            {
                return false;
            }

            return true;
        }

        private static T[] Read<T>(DirectorySearcher searcher, string propertyName)
        {

            SearchResult result;
            try
            {
                searcher.PropertiesToLoad.Add(string.Format(RangedAttributeFormat, propertyName, 0, "*"));
                result = searcher.FindOne();
            }
            catch (DirectoryServicesCOMException adsError)
            {
                if (adsError.ErrorCode == -2147016672)
                {
                    searcher.PropertiesToLoad.Clear();
                    searcher.PropertiesToLoad.Add(propertyName);
                    result = searcher.FindOne();
                }
                else
                {
                    throw;
                }
            }

            var results = new List<T>();
            var resultPropertyName = GetResultName(result, propertyName);
            if (string.IsNullOrEmpty(resultPropertyName))
            {
                return results.ToArray();
            }
            results.AddRange(result.Properties[resultPropertyName].Cast<T>().ToArray());

            int start, end;
            if (!ParseRangedPropertyName(resultPropertyName, out start, out end))
            {
                return results.ToArray();
            }
            if (end == int.MaxValue)
            {
                return results.ToArray();
            }

            var lastRange = false;
            while (!lastRange)
            {
                searcher.PropertiesToLoad.Clear();
                searcher.PropertiesToLoad.Add(string.Format(RangedAttributeFormat, propertyName, results.Count, "*"));
                try
                {
                    result = searcher.FindOne();
                }
                catch (DirectoryServicesCOMException ex)
                {
                    if (ex.ErrorCode == -2147016672)
                    {
                        break;
                    }
                    throw;
                }

                resultPropertyName = GetResultName(result, propertyName);
                if (string.IsNullOrEmpty(resultPropertyName)) continue;

                var values = result.Properties[resultPropertyName].Cast<T>().ToArray();
                results.AddRange(values);
                if (values.Length == 0)
                    lastRange = true;
            }

            return results.ToArray();
        }

        private static SearchResultCollection CreateSearcher(DirectoryEntry directoryEntry)
        {
            try
            {
                return new DirectorySearcher(directoryEntry)
                {
                    Filter = Config.Filter,
                    PageSize = 200
                }.FindAll();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("PageSize"))
                {
                    // Extension de la norme LDAP v3 non implémenté 
                    // RFC 2696 - LDAP Simple Paged Result Control
                    // La propriété Message doit-être égale à "La valeur de la propriété PageSize ne peut pas être définie"
                    // Ex: IBM(Lotus) Domino

                    return new DirectorySearcher(directoryEntry)
                    {
                        Filter = Config.Filter
                    }.FindAll();
                }
                throw;
            }
        }

        private static void Configuration(DirectoryEntry directoryEntry)
        {
            if (!string.IsNullOrEmpty(Config.Path)) directoryEntry.Path = Config.Path;
            if (!string.IsNullOrEmpty(Config.Login))
            {
                directoryEntry.Username = Config.Login;
                if (!string.IsNullOrEmpty(Config.Password)) directoryEntry.Password = Config.Password;
            }
            else
            {
                directoryEntry.AuthenticationType = AuthenticationTypes.None;
            }
        }
    }
}
