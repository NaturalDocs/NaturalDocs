/* 
 * Class: CodeClear.NaturalDocs.Engine.CommandLine
 * ____________________________________________________________________________
 * 
 * A class to handle command line parsing.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine
	{
	public class CommandLine
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: CommandLine
		 */
		public CommandLine (string[] commandLineSegments)
			{
			segments = commandLineSegments;
			index = 0;
			aliases = new StringToStringTable();
			}
			
		
		/* Function: AddAliases
		 * Registers command line aliases, such as "-i" and "--input".  The first one is the one that will be returned by <GetParameter()>.
		 */
		public void AddAliases (string parameter, params string[] aliases)
			{
			string lcParameter = parameter.ToLower();

			foreach (string alias in aliases)
				{  this.aliases[alias.ToLower()] = lcParameter;  }
			}



		// Group: Basic Parsing Functions
		// __________________________________________________________________________


		/* Function: GetParameter
		 * 
		 * Attempts to return a command line parameter, like "-i".  Will return false if the current position is not on a parameter.
		 * 
		 * parameter will include the dashes and always be in all lowercase.  Aliases added with <AddAliases()> will be replaced 
		 * automatically.
		 * 
		 * parameterAsEntered will return the parameter exactly as it was entered on the command line.
		 */
		public bool GetParameter (out string parameter, out string parameterAsEntered)
			{
			if (!IsOnParameter)
				{
				parameter = null;
				parameterAsEntered = null;
				return false;
				}

			parameterAsEntered = segments[index];
			string lcParameterAsEntered = parameterAsEntered.ToLower();
			parameter = aliases[lcParameterAsEntered] ?? lcParameterAsEntered;

			index++;
			return true;
			}


		/* Function: GetBareWord
		 * Attempts to retrieve a single bare word from the command line, like "HTML" in "-o HTML [path]".  Returns false if the
		 * current position is not on a bare word.
		 */
		public bool GetBareWord (out string bareWord)
			{
			if (!IsOnBareWord)
				{
				bareWord = null;
				return false;
				}

			bareWord = segments[index];
			index++;

			return true;
			}


		/* Function: GetBareWordsToNextParameter
		 * Attempts to retrieve all the bare words between the current position and the next parameter as a single string.  Returns
		 * false if the current position is not on a bare word.
		 */
		public bool GetBareWordsToNextParameter (out string bareWords)
			{
			if (!IsOnBareWord)
				{
				bareWords = null;
				return false;
				}

			StringBuilder builder = new StringBuilder(segments[index]);
			index++;

			while (IsInBounds && !IsOnParameter)
				{
				builder.Append(' ');
				builder.Append(segments[index]);
				index++;
				}

			bareWords = builder.ToString();
			return true;
			}


		/* Function: GetQuotedWords
		 * Attempts to retrieve a quoted section of text.  The returned string will not include the quotes.  Returns false if the
		 * current position is not on quoted words.
		 */
		protected bool GetQuotedWords (out string quotedWords)
			{
			if (!IsOnQuotedSection)
				{
				quotedWords = null;
				return false;
				}

			int closingIndex = FindClosingQuote();

			if (closingIndex == -1)
				{
				StringBuilder contents = new StringBuilder();
				contents.Append(segments[index], 1, segments[index].Length - 1);
				index++;

				while (index < segments.Length)
					{  
					contents.Append(segments[index]);
					index++;
					}

				quotedWords = contents.ToString();
				return true;
				}

			else if (closingIndex == index)
				{
				quotedWords = segments[index].Substring(1, segments[index].Length - 2);
				index++;
				return true;
				}

			else // closingIndex > index
				{
				StringBuilder contents = new StringBuilder();
				contents.Append(segments[index], 1, segments[index].Length - 1);
				index++;

				while (index < closingIndex - 1)
					{  
					contents.Append(segments[index]);
					index++;
					}

				contents.Append(segments[index], 0, segments[index].Length - 1);
				index++;

				quotedWords = contents.ToString();
				return true;
				}
			}


		/* Function: GetPath
		 * 
		 * Attempts to retrieve a path from the command line.  Returns false if the current position is not on a path.
		 * 
		 * Paths can be quoted or unquoted.  Quotes will not be included in the returned path.  If unquoted, the path continues 
		 * until the next parameter or the end of the command line.
		 */
		public bool GetPath (out Path path)
			{
			if (!IsOnPath)
				{
				path = null;
				return false;
				}

			string pathString;

			// pathString will be null if either of these fail
			if (IsOnQuotedSection)
				{  GetQuotedWords(out pathString);  }
			else
				{  GetBareWordsToNextParameter(out pathString);  }

			if (pathString != null)
				{
				path = new Path(pathString);
				return true;
				}
			else
				{
				path = null;
				return false;
				}
			}


		/* Function: SkipToNextParameter
		 * Moves the position to the next parameter, skipping any values along the way.  Note that if the position is already on 
		 * a parameter it will NOT move.
		 */
		public void SkipToNextParameter ()
			{
			while (IsInBounds && !IsOnParameter)
				{
				if (IsOnQuotedSection)
					{  SkipQuotedSection();  }
				else
					{  index++;  }
				}
			}



		// Group: Value Parsing Functions
		// __________________________________________________________________________


		/* Function: NoValue
		 * Call this after <GetParameter()> if it must not be followed by a value.  If it is it will return true.
		 */
		public bool NoValue ()
			{
			return (!IsInBounds || IsOnParameter);
			}


		/* Function: GetIntegerValue
		 * Call this after <GetParameter()> if it must be followed by an integer.  If it is it will return true and the integer.  If there's
		 * any other value or no value following the parameter it will return false.
		 */
		public bool GetIntegerValue (out int integer)
			{
			int oldIndex = index;

			string integerString;

			if (!GetBareWord(out integerString))
				{
				integer = 0;
				return false;
				}

			if (!Int32.TryParse(integerString, out integer))
				{
				index = oldIndex;
				integer = 0;
				return false;
				}

			if (IsInBounds && !IsOnParameter)
				{
				index = oldIndex;
				integer = 0;
				return false;
				}

			return true;
			}


		/* Function: GetPathValue
		 * Call this after <GetParameter()> if it must be followed by a path.  If it is it will return true and the path.  If there's any 
		 * other value or no value following the parameter it will return false.
		 */
		public bool GetPathValue (out Path path)
			{
			int oldIndex = index;

			if (!GetPath(out path))
				{
				path = null;
				return false;
				}

			if (IsInBounds && !IsOnParameter)
				{
				index = oldIndex;
				path = null;
				return false;
				}

			return true;
			}


		/* Function: GetBareWordAndPathValue
		 * Call this after <GetParameter()> if it must be followed by a bare word and then a path, such as "-o [format] [folder]".  If it 
		 * is it will return true and the values.  If it's not in the correct format it will return false.
		 */
		public bool GetBareWordAndPathValue (out string bareWord, out Path path)
			{
			int oldIndex = index;

			if (!GetBareWord(out bareWord))
				{
				bareWord = null;
				path = null;
				return false;
				}

			if (!GetPath(out path))
				{
				index = oldIndex;
				bareWord = null;
				path = null;
				return false;
				}

			if (IsInBounds && !IsOnParameter)
				{
				index = oldIndex;
				bareWord = null;
				path = null;
				return false;
				}

			return true;
			}


		/* Function: GetBareOrQuotedWordsValue
		 * Call this after <GetParameter()> if it must be followed by bare words or a quoted string.  If it is it will return true and the values.
		 * Quotes will not be included in the result, and unquoted words will be retrieved until the next parameter.  If it's not in the correct 
		 * format it will return false.
		 */
		public bool GetBareOrQuotedWordsValue (out string words)
			{
			int oldIndex = index;
			bool success;

			if (IsOnQuotedSection)
				{  success = GetQuotedWords(out words);  }
			else
				{  success = GetBareWordsToNextParameter(out words);  }

			if (!success)
				{
				words = null;
				return false;
				}

			if (IsInBounds && !IsOnParameter)
				{
				index = oldIndex;
				words = null;
				return false;
				}

			return true;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindClosingQuote
		 * Returns the index of the segment with a closing quote, assuming <index> is on a segment with an opening quote.
		 * Will return -1 if there isn't one.
		 */
		protected int FindClosingQuote ()
			{
			#if DEBUG
			if (index >= segments.Length || segments[index][0] != '"')
				{  throw new Exception ("Tried to call FindClosingQuote when the index was not on a segment with an opening quote.");  }
			#endif

			int endIndex = index;

			// If the first segment is nothing but a single quote, we don't want to mistake it for a closing quote as well.
			if (segments[index].Length == 1)
				{  endIndex++;  }

			while (endIndex < segments.Length)
				{
				string segment = segments[endIndex];

				if (segment[ segment.Length - 1] == '"')
					{  return endIndex;  }

				endIndex++;
				}

			return -1;
			}


		/* Function: SkipQuotedSection
		 * Moves <index> past the entire quoted section, assuming <index> is currently on a segment which starts with a quote.
		 */
		protected void SkipQuotedSection ()
			{
			#if DEBUG
			if (index >= segments.Length || segments[index][0] != '"')
				{  throw new Exception ("Tried to call SkipQuotedSection when the index was not on a segment with an opening quote.");  }
			#endif

			int closingIndex = FindClosingQuote();

			if (closingIndex == -1)
				{  index = segments.Length;  }
			else
				{  index = closingIndex + 1;  }
			}
			
			
		
		// Group: Properties
		// __________________________________________________________________________


		/* Property: InBounds
		 * Whether the current position is in bounds.
		 */
		public bool IsInBounds
			{
			get
				{  return (index < segments.Length);  }
			}


		/* Property: IsOnParameter
		 * Whether the current position is on a parameter.
		 */
		public bool IsOnParameter
			{
			get
				{  return (IsInBounds && segments[index][0] == '-');  }
			}


		/* Property: IsOnQuotedSection
		 * Whether the current position is on the beginning of a quoted section.
		 */
		public bool IsOnQuotedSection
			{
			get
				{  return (IsInBounds && segments[index][0] == '"');  }
			}


		/* Property: IsOnBareWord
		 * Whether the current position is on a bare word, meaning not a parameter or a quoted string.
		 */
		public bool IsOnBareWord
			{
			get
				{  return (IsInBounds && segments[index][0] != '-' && segments[index][0] != '"');  }
			}


		/* Property: IsOnPath
		 * Whether the current position is on something that can be interpreted as a path, either a bare word or a quoted
		 * string.
		 */
		public bool IsOnPath
			{
			get
				{  return (IsInBounds && segments[index][0] != '-');  }
			}



		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: segments
		 * The original command line as an array of strings
		 */
		protected string[] segments;
		
		/* var: index
		 * The current position as an index into <segments>.
		 */
		protected int index;

		/* var: aliases
		 * A list of command line aliases, such as "-i" and "--input".
		 */
		protected StringToStringTable aliases;
										
		}
	}