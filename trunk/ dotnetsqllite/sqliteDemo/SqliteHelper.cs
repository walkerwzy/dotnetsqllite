using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Collections;
using System.Data.SQLite;

namespace DBUtility.SQLite
{
    /// <summary>
    /// SQLiteHelper is a utility class similar to "SQLHelper" in MS
    /// Data Access Application Block and follows similar pattern.
    /// </summary>
    public class SQLiteHelper
    {
        private static string connStr = "Data Source=sqlitedemo.sqlite;Pooling=true;FailIfMissing=false";

        /// <summary>
        /// Creates a new <see cref="SQLiteHelper"/> instance. The ctor is marked private since all members are static.
        /// </summary>
        private SQLiteHelper()
        {
        }

        #region helper
        /// <summary>
        /// Parses parameter names from SQL Statement, assigns values from object array ,   /// and returns fully populated ParameterCollection.
        /// </summary>
        /// <param name="commandText">Sql Statement with "@param" style embedded parameters</param>
        /// <param name="paramList">object[] array of parameter values</param>
        /// <returns>SQLiteParameterCollection</returns>
        /// <remarks>Status experimental. Regex appears to be handling most issues. Note that parameter object array must be in same ///order as parameter names appear in SQL statement.</remarks>
        private static SQLiteParameterCollection AttachParameters(SQLiteCommand cmd, string commandText, params  object[] paramList)
        {
            if (paramList == null || paramList.Length == 0) return null;

            SQLiteParameterCollection coll = cmd.Parameters;
            string parmString = commandText.Substring(commandText.IndexOf("@"));
            // pre-process the string so always at least 1 space after a comma.
            parmString = parmString.Replace(",", " ,");
            // get the named parameters into a match collection
            string pattern = @"(@)\S*(.*?)\b";
            Regex ex = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection mc = ex.Matches(parmString);
            string[] paramNames = new string[mc.Count];
            int i = 0;
            foreach (Match m in mc)
            {
                paramNames[i] = m.Value;
                i++;
            }

            // now let's type the parameters
            int j = 0;
            Type t = null;
            foreach (object o in paramList)
            {
                t = o.GetType();

                SQLiteParameter parm = new SQLiteParameter();
                switch (t.ToString())
                {

                    case ("DBNull"):
                    case ("Char"):
                    case ("SByte"):
                    case ("UInt16"):
                    case ("UInt32"):
                    case ("UInt64"):
                        throw new SystemException("Invalid data type");


                    case ("System.String"):
                        parm.DbType = DbType.String;
                        parm.ParameterName = paramNames[j];
                        parm.Value = (string)paramList[j];
                        coll.Add(parm);
                        break;

                    case ("System.Byte[]"):
                        parm.DbType = DbType.Binary;
                        parm.ParameterName = paramNames[j];
                        parm.Value = (byte[])paramList[j];
                        coll.Add(parm);
                        break;

                    case ("System.Int32"):
                        parm.DbType = DbType.Int32;
                        parm.ParameterName = paramNames[j];
                        parm.Value = (int)paramList[j];
                        coll.Add(parm);
                        break;

                    case ("System.Boolean"):
                        parm.DbType = DbType.Boolean;
                        parm.ParameterName = paramNames[j];
                        parm.Value = (bool)paramList[j];
                        coll.Add(parm);
                        break;

                    case ("System.DateTime"):
                        parm.DbType = DbType.DateTime;
                        parm.ParameterName = paramNames[j];
                        parm.Value = Convert.ToDateTime(paramList[j]);
                        coll.Add(parm);
                        break;

                    case ("System.Double"):
                        parm.DbType = DbType.Double;
                        parm.ParameterName = paramNames[j];
                        parm.Value = Convert.ToDouble(paramList[j]);
                        coll.Add(parm);
                        break;

                    case ("System.Decimal"):
                        parm.DbType = DbType.Decimal;
                        parm.ParameterName = paramNames[j];
                        parm.Value = Convert.ToDecimal(paramList[j]);
                        break;

                    case ("System.Guid"):
                        parm.DbType = DbType.Guid;
                        parm.ParameterName = paramNames[j];
                        parm.Value = (System.Guid)(paramList[j]);
                        break;

                    case ("System.Object"):

                        parm.DbType = DbType.Object;
                        parm.ParameterName = paramNames[j];
                        parm.Value = paramList[j];
                        coll.Add(parm);
                        break;

                    default:
                        throw new SystemException("Value is of unknown data type");

                } // end switch

                j++;
            }
            return coll;
        }
        #endregion

        #region ExecuteDataSet

        /// <summary>
        /// Shortcut method to execute dataset from SQL Statement and object[] arrray of parameter values
        /// </summary>
        /// <param name="commandText">SQL Statement with embedded "@param" style parameter names</param>
        /// <param name="paramList">object[] array of parameter values</param>
        /// <returns></returns>
        public static DataSet ExecuteDataSet(string commandText,params object[] paramList)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    try
                    {
                        conn.Open();
                        if (paramList != null)
                        {
                            AttachParameters(cmd, commandText, paramList);
                        }
                        DataSet ds = new DataSet();
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        da.Fill(ds);
                        da.Dispose();
                        cmd.Dispose();
                        conn.Close();
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static DataSet ExecuteDataSet(string commandText)
        {
            return ExecuteDataSet(commandText, null);
        }

        /// <summary>
        /// Executes the dataset with Transaction and object array of parameter values.
        /// </summary>
        /// <param name="commandText">Command text.</param>
        /// <param name="commandParameters">object[] array of parameter values.</param>
        /// <returns>DataSet</returns>
        /// <remarks>user must examine Transaction Object and handle transaction.connection .Close, etc.</remarks>
        public static DataSet ExecuteDatasetWithTransaction(string commandText,params object[] commandParameters)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    SQLiteTransaction st = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                    try
                    {
                        DataSet ds = ExecuteDataSet(commandText, commandParameters);
                        st.Commit();
                        return ds;
                    }
                    catch (Exception ex)
                    {
                        st.Rollback();
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static DataSet ExecuteDatasetWithTransaction(string commandText)
        {
            return ExecuteDatasetWithTransaction(commandText, null);
        }
        #endregion

        #region ExecuteReader

        /// <summary>
        /// ShortCut method to return IDataReader
        /// NOTE: You should explicitly close the Command.connection you passed in as
        /// well as call Dispose on the Command  after reader is closed.
        /// We do this because IDataReader has no underlying Connection Property.
        /// </summary>
        /// <param name="commandText">SQL Statement with optional embedded "@param" style parameters</param>
        /// <param name="paramList">object[] array of parameter values</param>
        /// <returns>IDataReader</returns>
        public static IDataReader ExecuteReader(string commandText,params object[] paramList)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    try
                    {
                        if (paramList != null) AttachParameters(cmd, commandText, paramList);
                        IDataReader rdr = cmd.ExecuteReader();
                        return rdr;
                    }
                    catch (Exception ex)
                    {
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static IDataReader ExecuteReader(string commandText)
        {
            return ExecuteReader(commandText, null);
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        /// Shortcut to ExecuteNonQuery with SqlStatement and object[] param values
        /// </summary>
        /// <param name="commandText">Sql Statement with embedded "@param" style parameters</param>
        /// <param name="paramList">object[] array of parameter values</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string commandText, params object[] paramList)
        {

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    try
                    {
                        if (paramList != null) AttachParameters(cmd, commandText, paramList);
                        return cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static int ExecuteNonQuery(string commandText)
        {
            return ExecuteNonQuery(commandText, null);
        }

        /// <summary>
        /// Executes  non-query sql Statment with Transaction
        /// </summary>
        /// <param name="commandText">Command text.</param>
        /// <param name="paramList">Param list.</param>
        /// <returns>Integer</returns>
        /// <remarks>user must examine Transaction Object and handle transaction.connection .Close, etc.</remarks>
        public static int ExecuteNonQueryWithTransaction(string commandText, params  object[] paramList)
        {

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    SQLiteTransaction st = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                    try
                    {
                        if (paramList != null) AttachParameters((SQLiteCommand)cmd, cmd.CommandText, paramList);
                        int result = cmd.ExecuteNonQuery();
                        st.Commit();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        st.Rollback();
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static int ExecuteNonQueryWithTransaction(string commandTex)
        {
            return ExecuteNonQueryWithTransaction(commandTex, null);
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// Shortcut to ExecuteScalar with Sql Statement embedded params and object[] param values
        /// </summary>
        /// <param name="commandText">SQL statment with embedded "@param" style parameters</param>
        /// <param name="paramList">object[] array of param values</param>
        /// <returns></returns>
        public static object ExecuteScalar(string commandText, params  object[] paramList)
        {

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    try
                    {
                        if (paramList != null) AttachParameters(cmd, commandText, paramList);
                        return cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static object ExecuteScalar(string commandText)
        {
            return ExecuteScalar(commandText, null);
        }

        #endregion

        #region ExecuteXmlReader
        /// <summary>
        /// Execute XmlReader with complete Command
        /// </summary>
        /// <param name="command">querry string</param>
        /// <returns>XmlReader</returns>
        public static XmlReader ExecuteXmlReader(string commandText, params object[] paramList)
        {

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                {
                    conn.Open();
                    try
                    {
                        if (paramList != null) AttachParameters(cmd, commandText, paramList);
                        // get a data adapter  
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        // fill the data set, and return the schema information
                        da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                        da.Fill(ds);
                        // convert our dataset to XML
                        StringReader stream = new StringReader(ds.GetXml());
                        // convert our stream of text to an XmlReader
                        return new XmlTextReader(stream);
                    }
                    catch (Exception ex)
                    {
                        cmd.Dispose();
                        conn.Close();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        public static XmlReader ExecuteXmlReader(string commandText)
        {
            return ExecuteXmlReader(commandText,null);
        }

        #endregion
        
        #region UpdateDataset
        /// <summary>
        /// Executes the respective command for each inserted, updated, or deleted row in the DataSet.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  UpdateDataset(conn, insertCommand, deleteCommand, updateCommand, dataSet, "Order");
        /// </remarks>
        /// <param name="insertCommand">A valid SQL statement  to insert new records into the data source</param>
        /// <param name="deleteCommand">A valid SQL statement to delete records from the data source</param>
        /// <param name="updateCommand">A valid SQL statement used to update records in the data source</param>
        /// <param name="dataSet">The DataSet used to update the data source</param>
        /// <param name="tableName">The DataTable used to update the data source.</param>
        public static void UpdateDataset(SQLiteCommand insertCommand, SQLiteCommand deleteCommand, SQLiteCommand updateCommand, DataSet dataSet, string tableName)
        {
            if (insertCommand == null) throw new ArgumentNullException("insertCommand");
            if (deleteCommand == null) throw new ArgumentNullException("deleteCommand");
            if (updateCommand == null) throw new ArgumentNullException("updateCommand");
            if (tableName == null || tableName.Length == 0) throw new ArgumentNullException("tableName");

            // Create a SQLiteDataAdapter, and dispose of it after we are done
            using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter())
            {
                // Set the data adapter commands
                dataAdapter.UpdateCommand = updateCommand;
                dataAdapter.InsertCommand = insertCommand;
                dataAdapter.DeleteCommand = deleteCommand;

                // Update the dataset changes in the data source
                dataAdapter.Update(dataSet, tableName);

                // Commit all the changes made to the DataSet
                dataSet.AcceptChanges();
            }
        }
        #endregion

    }
}