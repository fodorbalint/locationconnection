using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	/*Retrieves a list of one-level depth objects.
	Spaces internally are not allowed.
	Backslash at the end of strings are not allowed.
	Property names are not enclosed in quotes.*/
	class ServerParser<T>
	{
		public List<T> returnCollection;

		public ServerParser(string inputString)
		{
			List<object> collection = new List<object>();
			int seekPos = 0;
			do
			{
				ExpandoObject obj = new ExpandoObject();
				do
				{
					int colonPos = inputString.IndexOf(':', seekPos + 1); //start from first char of property
					string key = inputString.Substring(seekPos + 1, colonPos - seekPos - 1);
					string value;
					if (inputString[colonPos + 1] == '"') //must not be a space after the colon
					{
						int valueSeekStart = colonPos + 2;
						int quotaPos;
						do
						{
							quotaPos = inputString.IndexOf('"', valueSeekStart);
							valueSeekStart = quotaPos + 1;
						} while (inputString[quotaPos - 1] == '\\'); //end character of the enclosed string cannot be backslash for this to work
						value = inputString.Substring(colonPos + 2, quotaPos - colonPos - 2);
						seekPos = quotaPos + 1; //space is not allowed after the closing quote                    
					}
					else
					{
						int testSeparatorPos1 = inputString.IndexOf(',', colonPos + 1); //for the ending } it returns -1
						int testSeparatorPos2 = inputString.IndexOf('}', colonPos + 1);
						if (testSeparatorPos1 != -1)
						{
							seekPos = (testSeparatorPos1 < testSeparatorPos2) ? testSeparatorPos1 : testSeparatorPos2;
						}
						else
						{
							seekPos = testSeparatorPos2;
						}
						value = inputString.Substring(colonPos + 1, seekPos - colonPos - 1);
					}
					AddProperty(obj, key, value);
				} while (inputString[seekPos] != '}');
				seekPos = seekPos + 1; //opening { of next object or after the last char of inputString
				collection.Add(obj);
			} while (seekPos < inputString.Length); //will be equal in the end

			switch (typeof(T).Name)
			{
				case "Session":
					foreach (object obj in collection)
					{
						var objDict = obj as IDictionary<string, object>;
						Type type = typeof(T);
						foreach (var kv in objDict)
						{
							FieldInfo fieldInfo = type.GetField(kv.Key);
							if (!(fieldInfo is null))
							{
								Type type1 = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
								object safeValue;
								if (kv.Key == "Pictures")
								{
									safeValue = ((string)kv.Value).Split("|");
								}
								else
								{
									safeValue = ((string)kv.Value == "") ? null : Convert.ChangeType(kv.Value, type1, CultureInfo.InvariantCulture);
								}
								fieldInfo.SetValue(null, safeValue);
							}
						}
					}
					break;
				case "Profile":
					returnCollection = new List<T>();
					foreach (object obj in collection)
					{
						var objDict = obj as IDictionary<string, object>;
						Type type = typeof(T);
						T profileItem = (T)Activator.CreateInstance(type);
						foreach (var kv in objDict)
						{
					   	    FieldInfo fieldInfo = type.GetField(kv.Key);
							if (!(fieldInfo is null))
							{
								Type type1 = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
								object safeValue;
								if (kv.Key == "Pictures")
								{
									safeValue = ((string)kv.Value).Split("|");
								}
								else
								{
									safeValue = ((string)kv.Value == "") ? null : Convert.ChangeType(kv.Value, type1, CultureInfo.InvariantCulture);
								}
								fieldInfo.SetValue(profileItem, safeValue);
							}
						}
						returnCollection.Add(profileItem);
					}
					break;
				case "MatchItem":
					returnCollection = new List<T>();				
					foreach (object obj in collection)
					{
						var objDict = obj as IDictionary<string, object>;
						Type type = typeof(T);
						T matchItem = (T)Activator.CreateInstance(type);
						foreach (var kv in objDict)
						{
							FieldInfo fieldInfo = type.GetField(kv.Key);
							if (!(fieldInfo is null))
							{
								Type type1 = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
								object safeValue;
								if (kv.Key == "Chat")
								{
									if ((string)kv.Value != "")
									{
										string chatStr = ((string)kv.Value).Substring(1, ((string)kv.Value).Length - 2);
										safeValue = chatStr.Split("}{");
									}
									else
									{
										safeValue = new string[0];
									}
								}
								else
								{
									safeValue = ((string)kv.Value == "") ? null : Convert.ChangeType(kv.Value, type1, CultureInfo.InvariantCulture);
								}
								fieldInfo.SetValue(matchItem, safeValue);
							}
						}
						returnCollection.Add(matchItem);
					}
					break;
				default:					
					break;
			}
		}

		public void AddProperty(ExpandoObject expando, string name, object value)
		{
			var exDict = expando as IDictionary<string, object>;
			if (exDict.ContainsKey(name))
				exDict[name] = value;
			else
				exDict.Add(name, value);
		}

		/*private object CreateInstance(string className)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var type = assembly.GetTypes()
				.First(t => t.Name == className);
			return Activator.CreateInstance(type);
		}*/
	}
}