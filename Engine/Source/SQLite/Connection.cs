/*
 * Class: CodeClear.NaturalDocs.Engine.SQLite.Connection
 * ____________________________________________________________________________
 *
 * A class to provide access to a SQLite database connection.
 *
 *
 * Topic: Requirements
 *
 *		- The class must have access to NaturalDocs.Engine.SQLite.dll on Windows and libNaturalDocs.Engine.SQLite.so
 *		  on Linux.  Both must be compiled in multithreaded mode.
 *		- All threads must have their own connection, even if it's to the same database.  They cannot be shared.  One
 *		  thread can pass a connection to another, though.
 *		- Any <SQLite.Queries> and related objects have to be disposed of before disposing of the connection.
 *		- Any <SQLite.Queries> and related objects should only be used in the same thread as the connection.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		The underlying database is thread safe, but Connection objects are not.  Each thread needs to have
 *		its own Connection object to access the database.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

#if !SQLITE_UTF8 && !SQLITE_UTF16
	#define SQLITE_UTF8
#endif

using System;


namespace CodeClear.NaturalDocs.Engine.SQLite
	{
	public class Connection : IDisposable
		{

		// Group: Functions
		// ___________________________________________________________________________


		/* Constructor: Connection
		 */
		public Connection ()
			{
			handle = IntPtr.Zero;
			databaseFile = null;
			statementByteLengthLimit = -1;
			argumentLimit = -1;
			}

		/* Destructor: ~Connection
		 */
		 ~Connection ()
			{
			Dispose(true);
			}


		/* Function: Open
		 *
		 * Opens a connection to a database file, possibly creating one if necessary.  Throws an exception if it fails.
		 *
		 * Parameters:
		 *
		 *		filename - The <Path> of the file to open.
		 *		createIfDoesntExist - If true will create the file if it doesn't exist already.
		 */
		public void Open (Path filename, bool createIfDoesntExist)
			{
			if (IsOpen)
				{  throw new Exception("Tried to open a database connection while the object already had one open.");  }

			// NoMutex has support for multithreading but connections are not serialized.  Since we're not going to be sharing connections
			// and queries between threads, we don't need serialization.
			API.OpenOption options = API.OpenOption.ReadWrite | API.OpenOption.NoMutex;

			if (createIfDoesntExist)
				{  options |= API.OpenOption.Create;  }

			API.Result sqliteResult = API.OpenV2(filename, out handle, options);
			databaseFile = filename;

			try
				{
				if (sqliteResult != API.Result.OK)
					{
					throw new Exceptions.UnexpectedResult ("Could not open " + (createIfDoesntExist ? "or create " : "") +
																		  "database connection to " + filename, sqliteResult);
					}

				// Don't check the result since it's not a big deal if this fails.
				API.ExtendedResultCodes(handle, true);

				// Even with proper locking it's possible for queries to return Busy, even when all the threads are only reading.  Alas.
				API.BusyTimeout(handle, BusyTimeoutInMS);

				// Query the limits
				// "Hey," you might be thinking, "why don't we just get these when we need them in the Query class?  Then we don't have to
				// store them here and pass a reference to the Connection object."  Well, you get a memory protection error when you try to
				// do this while building an exception in Query.  It works fine here though.
				statementByteLengthLimit = API.Limit(handle, API.LimitID.SQLLength, -1);
				argumentLimit = API.Limit(handle, API.LimitID.FunctionArguments, -1);

				// No point in using SQLite's case-insensitivity since it only applies to ASCII characters.
				Execute("PRAGMA case_sensitive_like = 1");

				// This allows SQLite to support transactions but keeps the rollback journal in memory instead of on disk.  Corruption
				// caused by a crash isn't a concern because the database is expendable and Natural Docs would rebuild everything the
				// next time it's run anyway.  We'd rather have the performance increase from the lack of disk access.
				Execute("PRAGMA journal_mode = MEMORY");

				// Make any new databases consistent with the API.  It will work even with a mismatch, but it will perform better if it's consistent.
				#if SQLITE_UTF16
					Execute("PRAGMA encoding = \"UTF-16\"");
				#elif SQLITE_UTF8
					Execute("PRAGMA encoding = \"UTF-8\"");
				#else
					throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
				#endif

				// Not turning this off means SQLite will force the operating system to flush the disk cache for the database file at various
				// points.  This means the program will have to pause while the OS commits all changes to disk instead of just counting on
				// the reliability of the OS's disk cache like almost every other program does.  The only corruption this could prevent is when
				// the entire OS crashes while database writes are still in its cache; just the program crashing is already covered.  That level
				// of paranoia is unnecessary here, and it's especially slow on Linux because it usually can't flush the cache for an individual
				// file, only the entire filesystem.  This cropped up in Firefox: http://shaver.off.net/diary/2008/05/25/fsyncers-and-curveballs/
				// or just Google "firefox fsync sqlite".
				Execute("PRAGMA synchronous = OFF");
				}
			catch
				{
				// We have to close the handle even if OpenV2() failed, according to the API.
				API.CloseV2(handle);
				handle = IntPtr.Zero;
				databaseFile = null;
				statementByteLengthLimit = -1;
				argumentLimit = -1;

				throw;
				}
			}


		/* Function: CreateAnotherConnection
		 * Creates a new connection to the same database as this one.  The connection is otherwise independent though, and as such
		 * isn't tied to the <Queries> and other objects of the original.  This is good for making connections for worker threads to use off
		 * the main one.  Throws an exception if it fails.
		 */
		public Connection CreateAnotherConnection ()
			{
			if (!IsOpen)
				{  throw new Exception("Tried to create another connection to a database when one wasn't open.");  }

			// Right now this is straightforward, but in the future it may be a more cut down version, such as one that doesn't set
			// as many pragmas.
			Connection result = new Connection();
			result.Open(databaseFile, false);

			return result;
			}


		/* Function: Close
		 *
		 * Closes the database connection.  It is okay to use <Dispose()> instead.
		 */
		public void Close ()
			{
			Dispose();
			}


		/* Function: Query
		 * Creates and returns a <SQLite.Query> object for the passed statement and values.
		 */
		public SQLite.Query Query (string statement, params Object[] values)
			{
			SQLite.Query query = new SQLite.Query();
			query.Prepare(this, statement, values);

			return query;
			}


		/* Function: Execute
		 * Executes a SQL statement where no result is required.
		 */
		public void Execute (string statement, params Object[] values)
			{
			using (SQLite.Query query = new SQLite.Query())
				{
				query.Prepare(this, statement, values);
				while (query.Step() == true)
					{  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsOpen
		 * Whether the connection is open.
		 */
		public bool IsOpen
			{
			get
				{  return (handle != IntPtr.Zero);  }
			}


		/* Property: DatabaseFile
		 * The path to the database file if one's open, or null if not.
		 */
		public Path DatabaseFile
			{
			get
				{  return databaseFile;  }
			}


		/* Property: Handle
		 * The SQLite database connection handle.
		 */
		internal IntPtr Handle
			{
			get
				{  return handle;  }
			}


		/* Property: StatementByteLengthLimit
		 * The maximum number of bytes in a SQL statement.
		 */
		public int StatementByteLengthLimit
			{
			get
				{  return statementByteLengthLimit;  }
			}


		/* Property: ArgumentLimit
		 * The maximum number of arguments that can be passed to a SQL statement.
		 */
		public int ArgumentLimit
			{
			get
				{  return argumentLimit;  }
			}



		// Group: IDisposable Functions
		// __________________________________________________________________________


		/* Function: Dispose
		 */
		public void Dispose ()
			{
			Dispose(false);
			}


		/* Function: Dispose
		 */
		protected void Dispose (bool strictRulesApply)
			{
			if (handle != IntPtr.Zero)
				{
				API.Result closeResult = API.CloseV2(handle);
				handle = IntPtr.Zero;
				databaseFile = null;
				statementByteLengthLimit = -1;
				argumentLimit = -1;

				if (strictRulesApply == false && closeResult != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not close database.", closeResult);  }
				}
			}




		// Group: Variables
		// __________________________________________________________________________


		/* Handle: handle
		 * The SQLite connection handle.  Will be IntPtr.Zero if not open.
		 */
		protected IntPtr handle;

		/* var: databaseFile
		 * The database file path.
		 */
		protected Path databaseFile;

		/* var: statementByteLengthLimit
		 * The maximum number of bytes in a SQL statement.
		 */
		protected int statementByteLengthLimit;

		/* var: argumentLimit
		 * The maximum number of arguments that can be passed to a SQL statement.
		 */
		protected int argumentLimit;



		// Group: Constants
		// __________________________________________________________________________


		/* Constant: BusyTimeoutInMS
		 */
		protected const int BusyTimeoutInMS = 2000;

		}
	}
