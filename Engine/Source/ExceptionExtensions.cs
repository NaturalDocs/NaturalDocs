/*
 * Class: CodeClear.NaturalDocs.Engine.ExceptionExtensions
 * ____________________________________________________________________________
 *
 * A static class for all the functions added to the System.Exception type.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine
	{
	static public class ExceptionExtensions
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: HasNaturalDocsTask
		 * Whether this exception has a Natural Docs task information attached to it.
		 */
		static public bool HasNaturalDocsTask (this Exception exception)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			return exceptionData.Contains("NDTask");
			}

		/* Function: AddNaturalDocsTask
		 * Adds a Natural Docs task information to the exception.  Multiple tasks can be added, such as "Processing file x" and "Processing topic y".
		 * Since it is assumed that these will be added by exception handlers, it is assumed that they will be added in order of lowest child task to
		 * highest parent task.
		 */
		static public void AddNaturalDocsTask (this Exception exception, string task)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			if (exceptionData.Contains("NDTask"))
				{  exceptionData["NDTask"] = task + '\n' + exceptionData["NDTask"];  }
			else
				{  exceptionData.Add("NDTask", task);  }
			}

		/* Function: GetNaturalDocsTasks
		 * Returns the task Natural Docs was working on when the exception was thrown, or null if none.  There may be subtasks, such as "Processing
		 * file x" and "Processing topic y" so it is returned as a list.  The list is ordered from the highest parent task to the lowest child task.
		 */
		static public IList<string> GetNaturalDocsTasks (this Exception exception)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			if (exceptionData.Contains("NDTask"))
				{  return exceptionData["NDTask"].ToString().Split('\n');  }
			else
				{  return null;  }
			}

		/* Function: HasNaturalDocsQuery
		 * Whether this exception has query information attached to it by Natural Docs.
		 */
		static public bool HasNaturalDocsQuery (this Exception exception)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			return exceptionData.Contains("NDQuery");
			}

		/* Function: SetNaturalDocsQuery
		 * Adds the Natural Docs query information to the exception.  Only the first query added will be stored.  Subsequent ones
		 * will be ignored.
		 */
		static public void AddNaturalDocsQuery (this Exception exception, string statement, params Object[] values)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			if (exceptionData.Contains("NDQuery") == false)
				{
				exceptionData.Add("NDQuery", statement);

				if (values != null && values.Length > 0)
					{
					List<string> valueStrings = new List<string>(values.Length);

					foreach (var value in values)
						{
						if (value == null)
							{  valueStrings.Add("null");  }
						else if (value is string)
							{  valueStrings.Add('"' + (string)value + '"');  }
						else if (value is byte ||
								  value is sbyte ||
								  value is short ||
								  value is ushort ||
								  value is int ||
								  value is uint ||
								  value is long ||
								  value is ulong ||
								  value is float ||
								  value is double ||
								  value is decimal)
							{  valueStrings.Add(value.ToString());  }
						else
							{
							string valueString = '(' + value.GetType().Name + ')';

							try
								{  valueString += value.ToString();  }
							catch
								{  }

							valueStrings.Add(valueString);
							}
						}

					exceptionData.Add("NDQueryValues", valueStrings);
					}
				}
			}

		/* Function: GetNaturalDocsQuery
		 * Returns the query Natural Docs was working on when the exception was thrown, or returns false if none.
		 */
		static public bool GetNaturalDocsQuery (this Exception exception, out string query, out List<string> values)
			{
			if (exception is Exceptions.Thread)
				{  exception = exception.InnerException;  }

			var exceptionData = exception.Data;

			if (exceptionData.Contains("NDQuery"))
				{
				query = exceptionData["NDQuery"].ToString();

				if (exceptionData.Contains("NDQueryValues"))
					{  values = (List<string>)exceptionData["NDQueryValues"];  }
				else
					{  values = null;  }

				return true;
				}
			else
				{
				query = null;
				values = null;
				return false;
				}
			}

		}
	}
