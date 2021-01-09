/* 
 * Class: CodeClear.NaturalDocs.Engine.SQLite.Query
 * ____________________________________________________________________________
 * 
 * An object representing a SQLite query.
 * 
 * 
 * Topic: Usage
 * 
 *		- These objects should not be created directly.  Use <SQLite.Connection> functions instead which will return
 *		  them.
 *		
 *		- Once a statement is ready, call <Step()> to get the first row, if any.  You can use functions like <IntColumn()> 
 *		  to retrieve values from the row at specific indexes, or functions like <NextIntColumn()> to iterate through them.
 *		  Continue calling <Step()> until you run out of rows or don't need any more.
 *		  
 *		- If desired, you can call <Reset()> at any time to start over from the beginning.  You can also have it clear
 *		  the bindings so you can call <BindValues()> to run it again with new values.
 *		  
 *		- <Dispose()> of the object when you are done with it.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		The underlying database is thread safe, but <Connection> and Query objects are not.  Each thread needs to have
 *		its own <Connection> object and use only Queries created by that object.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

#if !SQLITE_UTF8 && !SQLITE_UTF16
	#define SQLITE_UTF8
#endif

using System;


namespace CodeClear.NaturalDocs.Engine.SQLite
	{
	public class Query : IDisposable
		{
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Query
		 * Query objects should be created by <SQLite.Connection>, not directly.
		 */
		public Query ()
			{
			statementHandle = IntPtr.Zero;
			connection = null;
			columnIndex = 0;
			} 
			
		/* Destructor: ~Query
		 */
		~Query ()
			{
			Dispose(true);
			}
			
			
		/* Function: Prepare
		 * Prepares a SQL statement for execution.  If values are specified, <BindValues()> is called on them.  This
		 * function should only be called by <SQLite.Connection>.
		 */
		public void Prepare (Connection connection, string statement, params Object[] values)
			{
			if (statementHandle != IntPtr.Zero)
				{  throw new Exception("Tried to prepare a query when one already existed.");  }

			this.connection = connection;

			API.Result result = API.PrepareV2 (connection.Handle, statement, out statementHandle);
			
			if (result != API.Result.OK)
				{
				statementHandle = IntPtr.Zero;
				
				string errorMessage = "Could not prepare query.";


				// See if we ran afoul of any limits to make the error message more meaningful
				// Using SQLite.API.Limit() here causes memory protection errors, which is why we need to get it in the
				// Connection object and reference that here instead.

				int sqlByteLength;

				#if SQLITE_UTF16
				sqlByteLength = System.Text.Encoding.Unicode.GetByteCount(statement);
				#elif SQLITE_UTF8
				sqlByteLength = System.Text.Encoding.UTF8.GetByteCount(statement);
				#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
				#endif

				int valueCount = (values == null ? 0 : values.Length);

				if (sqlByteLength > connection.StatementByteLengthLimit && valueCount > connection.ArgumentLimit)
					{
					errorMessage += "  Statement is too long (byte count: " + sqlByteLength + ", limit: " + connection.StatementByteLengthLimit + ") and " +
											  "has too many arguments (count: " + valueCount + ", limit: " + connection.ArgumentLimit + ").";
					}
				else if (sqlByteLength > connection.StatementByteLengthLimit)
					{
					errorMessage += "  Statement is too long (byte count: " + sqlByteLength + ", limit: " + connection.StatementByteLengthLimit + ").";
					}
				else if (valueCount > connection.ArgumentLimit)
					{
					errorMessage += "  Query has too many arguments (count: " + valueCount + ", limit: " + connection.ArgumentLimit + ").";
					}

				var exception = new Exceptions.UnexpectedResult(errorMessage, result);
				exception.AddNaturalDocsQuery(statement, values);

				connection = null;

				throw exception;
				}
				
			if (values != null && values.Length != 0)
				{  BindValues(values);  }
			}
			
			
		/* Function: BindValues
		 * 
		 * Binds values to any question marks in the SQL statement.  Any values that aren't integers, strings, or 
		 * doubles will have ToString called on them.
		 * 
		 * This function is called automatically by <Prepare()> so it usually won't need to be called manually.  
		 * However, if <Reset()> is called this may be called again to bind new values to the statement.
		 */
		public void BindValues (params Object[] values)
			{
			int index = 1;  // Indexes start at 1 for these API functions for some reason.
			API.Result result;
			
			foreach (Object value in values)
				{
				if (value == null)
					{  result = API.BindNull(statementHandle, index);  }
				else if (value is int)
					{  result = API.BindInt(statementHandle, index, (int)value);  }
				else if (value is string)
					{  result = API.BindText(statementHandle, index, (string)value);  }  
				else if (value is long)
					{  result = API.BindInt64(statementHandle, index, (long)value);  }
				else if (value is double)
					{  result = API.BindDouble(statementHandle, index, (double)value);  }
				else
					{  
					string stringValue = value.ToString();

					if (stringValue == null)
						{  result = API.BindNull(statementHandle, index);  }
					else
						{  result = API.BindText(statementHandle, index, stringValue);  }
					}
					
				if (result != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not bind value to statement", result);  }
					
				index++;
				}
			}
			
			
		/* Function: Step
		 * Executes the statement until it returns a row or completes.  Returns true if it returns a row or false if
		 * there are no more.  This also resets the column index used by functions like <NextIntColumn()>.
		 */
		public bool Step ()
			{
			API.Result result = API.Step(statementHandle);
			columnIndex = 0;
			
			if (result == API.Result.Row)
				{  return true;  }
			else if (result == API.Result.Done)
				{  return false;  }
			else
				{  throw new Exceptions.UnexpectedResult("Could not step query.", result);  }
			}
			
			
		/* Function: IntColumn
		 * Returns the value of an integer column in a row found by <Step()>.  The column indexes start at zero.
		 */
		public int IntColumn (int columnIndex)
			{  return API.ColumnInt(statementHandle, columnIndex);  }
			
		/* Function: StringColumn
		 * Returns the value of a string column in a row found by <Step()>.  The column indexes start at zero.
		 */
		public string StringColumn (int columnIndex)
			{  return API.ColumnText(statementHandle, columnIndex);  }
			
		/* Function: LongColumn
		 * Returns the value of a 64-bit integer column in a row found by <Step()>.  The column indexes start at zero.
		 */
		public long LongColumn (int columnIndex)
			{  return API.ColumnInt64(statementHandle, columnIndex);  }
			
		/* Function: DoubleColumn
		 * Returns the value of a double (floating point) column in a row found by <Step()>.  The column indexes 
		 * start at zero.
		 */
		public double DoubleColumn (int columnIndex)
			{  return API.ColumnDouble(statementHandle, columnIndex);  }
			
			
		/* Function: NextIntColumn
		 * Returns the integer value of the next column in a row found by <Step()>.  NextColumn functions start at the first
		 * column after each <Step()> and move to the next one on each call.
		 */
		public int NextIntColumn ()
			{  
			int result = IntColumn(columnIndex);
			columnIndex++;
			return result;
			}
			
		/* Function: NextStringColumn
		 * Returns the string value of the next column in a row found by <Step()>.  NextColumn functions start at the first
		 * column after each <Step()> and move to the next one on each call.
		 */
		public string NextStringColumn ()
			{  
			string result = StringColumn(columnIndex);
			columnIndex++;
			return result;
			}
			
		/* Function: NextLongColumn
		 * Returns the 64-bit integer value of the next column in a row found by <Step()>.  NextColumn functions start at 
		 * the first column after each <Step()> and move to the next one on each call.
		 */
		public long NextLongColumn ()
			{  
			long result = LongColumn(columnIndex);
			columnIndex++;
			return result;
			}
			
		/* Function: NextDoubleColumn
		 * Returns the double (floating point) value of the next column in a row found by <Step()>.  NextColumn functions 
		 * start at the first column after each <Step()> and move to the next one on each call.
		 */
		public double NextDoubleColumn ()
			{  
			double result = DoubleColumn(columnIndex);
			columnIndex++;
			return result;
			}
			
			
		/* Function: Reset
		 * Restarts the query from the beginning.  Can optionally clear its bindings.
		 */
		public void Reset (bool clearBindings)
			{
			API.Result result = API.Reset(statementHandle);
			
			if (result != API.Result.OK)
				{  throw new Exceptions.UnexpectedResult("Could not reset the query.", result);  }
				
			if (clearBindings == true)
				{
				result = API.ClearBindings(statementHandle);
				
				if (result != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not clear statement bindings.", result);  }
				}
			}
		
		
		
		// Group: IDisposable Functions
		// __________________________________________________________________________
		
		
		public void Dispose ()
			{
			Dispose(false);
			}
			
		protected void Dispose (bool strictRulesApply)
			{
			if (statementHandle != IntPtr.Zero)
				{
				// We don't need to worry about the return value from Finalize().  According to the SQLite docs, it returns
				// whatever the most recent evaluation of the statement returned, so an error here doesn't mean the
				// finalization failed.
				API.Finalize (statementHandle);

				statementHandle = IntPtr.Zero;
				connection = null;
				}	
			}
		
			
			
	
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* Handle: statementHandle
		 * A handle to the prepared SQLite statement.
		 */
		protected IntPtr statementHandle; 
		
		/* Handle: connectionHandle
		 * The <Connection> that created this query.
		 */
		protected Connection connection;

		/* var: columnIndex
		 * The current column index when using functions like <NextIntColumn()>.
		 */
		protected int columnIndex;

		}
	}