/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.NumberSet
 * ____________________________________________________________________________
 *
 * A class to test <Engine.IDObjects.NumberSet>.
 *
 * Commands:
 *
 *		> // text
 *		Comment.  Ignored.
 *
 *		> {1-9,12}
 *		Sets the NumberSet to that value.
 *
 *		> +4
 *		Adds that number to the set.
 *
 *		> -9
 *		Removes that number from the set.
 *
 *		> 21?
 *		Does the number exist in the set?
 *
 *		> +{4-5}
 *		Adds another set.
 *
 *		> -{8-9}
 *		Subtracts a set.
 *
 *		> Extract 1, 4
 *		Extracts the *ranges* at the index and count.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class NumberSet : Framework.TextCommands
		{

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();
			Engine.IDObjects.NumberSet set = new Engine.IDObjects.NumberSet();

			foreach (string command in commands)
				{
				var match = GetBracesRegex.Match(command);
				string braces = (match.Success ? match.Value : null);
				match = GetNumberRegex.Match(command);
				int number = (match.Success ? int.Parse(match.Value) : -1);
				match = ExtractRangeRegex.Match(command);
				int extractIndex, extractCount;

				if (match.Success)
					{
					extractIndex = int.Parse(match.Groups[1].ToString());
					extractCount = int.Parse(match.Groups[2].ToString());
					}
				else
					{
					extractIndex = -1;
					extractCount = -1;
					}

				bool showSet = false;
				output.AppendLine(command);

				try
					{
					if (command == "" || command.StartsWith("//"))
						{
						// Ignore
						}
					else if (command.StartsWith("{"))
						{
						set.SetTo(braces);
						showSet = true;
						}
					else if (command.StartsWith("+{"))
						{
						Engine.IDObjects.NumberSet temp = new Engine.IDObjects.NumberSet(braces);
						set.Add(temp);
						showSet = true;
						}
					else if (command.StartsWith("+"))
						{
						set.Add(number);
						showSet = true;
						}
					else if (command.StartsWith("-{"))
						{
						Engine.IDObjects.NumberSet temp = new Engine.IDObjects.NumberSet(braces);
						set.Remove(temp);
						showSet = true;
						}
					else if (command.StartsWith("-"))
						{
						set.Remove(number);
						showSet = true;
						}
					else if (command.EndsWith("?"))
						{
						output.AppendLine( (set.Contains(number) ? "true" : "false") );
						}
					else if (extractIndex != -1 && extractCount != -1)
						{
						Engine.IDObjects.NumberSet extracted = set.ExtractRanges(extractIndex, extractCount);
						output.AppendLine(extracted.ToString());
						}
					else
						{  throw new Exception("Unknown command " + command);  }
					}
				catch (Exception e)
					{
					output.AppendLine("Exception: " + e.Message);
					output.AppendLine("(" + e.GetType().ToString() + ")");
					set.Clear();
					showSet = true;
					}

				if (showSet)
					{
					output.AppendLine("= " + set.ToString() +
														 " Lowest Available: " + set.LowestAvailable + ", Highest Used: " + set.Highest + ", Count: " + set.Count);
					output.AppendLine();
					}
				}

			return output.ToString();
			}

		protected static Regex GetBracesRegex = new Regex(@"{.*?}",
																					 RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		protected static Regex GetNumberRegex = new Regex("[0-9]+",
																					   RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		protected static Regex ExtractRangeRegex = new Regex("Extract ([0-9]+), ?([0-9]+)",
																						 RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		}
	}
