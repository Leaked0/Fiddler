using System;
using System.Text.RegularExpressions;
using Fiddler;

namespace FiddlerCore.Common.Rules
{
	// Token: 0x020000B1 RID: 177
	internal class ValueRules
	{
		/// <summary>
		/// Match a string with a condition and a value
		/// </summary>
		/// <param name="condition">the condition to check - e.g. eq, contains</param>
		/// <param name="checkedValue">the string to check against</param>
		/// <param name="searchValue">the value to search for</param>
		// Token: 0x060006AD RID: 1709 RVA: 0x00036E84 File Offset: 0x00035084
		internal static bool SearchString(string condition, string checkedValue, string searchValue)
		{
			bool bothHaveValue = !string.IsNullOrEmpty(checkedValue) && !string.IsNullOrEmpty(searchValue);
			long checkedNumber = 0L;
			long searchNumber = 0L;
			bool bothAreInt = bothHaveValue && long.TryParse(checkedValue, out checkedNumber) && long.TryParse(searchValue, out searchNumber);
			uint num = <PrivateImplementationDetails>.ComputeStringHash(condition);
			if (num <= 1462861033U)
			{
				if (num <= 1033122840U)
				{
					if (num != 714709303U)
					{
						if (num != 790464931U)
						{
							if (num == 1033122840U)
							{
								if (condition == "lte")
								{
									return bothAreInt && checkedNumber <= searchNumber;
								}
							}
						}
						else if (condition == "endswith")
						{
							return bothHaveValue && checkedValue.EndsWith(searchValue);
						}
					}
					else if (condition == "neq")
					{
						return checkedValue != searchValue;
					}
				}
				else if (num != 1142581827U)
				{
					if (num != 1260422518U)
					{
						if (num == 1462861033U)
						{
							if (condition == "gte")
							{
								return bothAreInt && checkedNumber >= searchNumber;
							}
						}
					}
					else if (condition == "gt")
					{
						return bothAreInt && checkedNumber > searchNumber;
					}
				}
				else if (condition == "eq")
				{
					return checkedValue == searchValue;
				}
			}
			else if (num <= 1825239352U)
			{
				if (num != 1563552493U)
				{
					if (num != 1623718223U)
					{
						if (num == 1825239352U)
						{
							if (condition == "contains")
							{
								return bothHaveValue && checkedValue.IndexOf(searchValue) != -1;
							}
						}
					}
					else if (condition == "isnotempty")
					{
						return !string.IsNullOrEmpty(checkedValue);
					}
				}
				else if (condition == "lt")
				{
					return bothAreInt && checkedNumber < searchNumber;
				}
			}
			else if (num <= 4031440047U)
			{
				if (num != 3876688446U)
				{
					if (num == 4031440047U)
					{
						if (condition == "doesnotcontain")
						{
							return bothHaveValue && checkedValue.IndexOf(searchValue) == -1;
						}
					}
				}
				else if (condition == "isempty")
				{
					return string.IsNullOrEmpty(checkedValue);
				}
			}
			else if (num != 4152769688U)
			{
				if (num == 4221853948U)
				{
					if (condition == "startswith")
					{
						return bothHaveValue && checkedValue.StartsWith(searchValue);
					}
				}
			}
			else if (condition == "regexp")
			{
				return bothHaveValue && Regex.IsMatch(checkedValue, searchValue);
			}
			FiddlerApplication.Log.LogFormat("Unknown compare condition \"{0}\"", new object[] { condition });
			return false;
		}
	}
}
