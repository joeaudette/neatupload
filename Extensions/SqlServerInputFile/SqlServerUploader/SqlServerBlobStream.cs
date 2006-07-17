/*
 
SqlServerUploader - an addon to NeatUpload to allow uploading files to stream
directly into a database.
Copyright (C) 2006  Joakim Wennergren (jokedst@gmail.com)

NeatUpload is an HttpModule and User Controls for uploading large files.
NeatUpload is created and maintained by Dean Brettle (www.brettle.com)

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Hitone.Web.SqlServerUploader
{
    /// <summary>
    /// Stream that writes directly into an SQL Server "Image"-column 
    /// </summary>
    public class SqlServerBlobStream : Stream
    {
        public override bool CanRead { get { return _access == FileAccess.ReadWrite || _access == FileAccess.Read; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return _access == FileAccess.ReadWrite || _access == FileAccess.Write; } }

        private SqlConnection _connection = null;
        private long _offset = 0;
        private int _identity = -1;
        private byte[] _pointer = null;

        private FileAccess _access = FileAccess.ReadWrite;
        private bool _isOpen = false;
        private bool _modified = false;
        private bool _disposed = false;

        // A set of SqlCommands, prepared for usage from the beginning
        private SqlCommand _writeCommand = null;
        private SqlCommand _readCommand = null;
        private SqlCommand _cleanupCommand = null;
        private SqlCommand _deleteCommand = null;

        private string _fileName = null;
        private string _MIMEType = null;
        public string FileName { get { return _fileName; } }
        public string MIMEType { get { return _MIMEType; } }

        /// <summary>
        /// Gets the identity column value for the created row in the database
        /// </summary>
        public int Identity { get { return _identity; } }


        /// <summary>
        /// Main constructor; creates a SqlServerBlobStream agains given database using given settings
        /// </summary>
        /// <param name="ConnectionString">Connections string agains the database to use</param>
        /// <param name="CreatorProcedure"><c>Optional</c> Procedure to use when creating the file-containing row in the database</param>
        /// <param name="TableName">Name of table to which we write the streamed data</param>
        /// <param name="DataColumnName">Name of table column to store data into (usually of type <c>Text</c> or <c>Image</c>)</param>
        /// <param name="PartialFlagColumnName"><c>Optional</c> Name of table column to store a "partial"-flag in; while uploading this will be set to 1 (or set by CreatorProcedure), when done it will be set to 0</param>
        /// <param name="FileNameColumnName"><c>Optional</c> Name of table column where the name of the uploaded file will be stored</param>
        /// <param name="FileName"><c>Optional</c> Name of the uploaded file</param>
        /// <param name="MIMETypeColumnName"><c>Optional</c> Name of table column where the MIME-type of the uploaded file will be stored</param>
        /// <param name="MIMEType"><c>Optional</c> MIME-type of the uploaded file</param>
        public SqlServerBlobStream(string ConnectionString,  string TableName, string DataColumnName, string PartialFlagColumnName, string FileNameColumnName, string FileName, string MIMETypeColumnName, string MIMEType,
             string createProcedure, string openProcedure, string writeProcedure, string readProcedure, string cleanupProcedure, string renameProcedure, string storeHashProcedure, string deleteProcedure)
        {
            Initialize(ConnectionString, TableName, DataColumnName, PartialFlagColumnName, FileNameColumnName, FileName, MIMETypeColumnName, MIMEType,
                         createProcedure, openProcedure, writeProcedure, readProcedure, cleanupProcedure, renameProcedure, storeHashProcedure, deleteProcedure);

        }

        /// <summary>
        /// Opens an exising blob for read/write access
        /// </summary>
        /// <param name="tableName">Name of table from which to retreive data</param>
        /// <param name="dataColumnName">Name of column from which to retreive data</param>
        /// <param name="identity">Identity of row to open</param>
        /// <param name="fileNameColumnName">If specified the filename will be retrieved from the database</param>
        /// <param name="MIMETypeColumnName">If specified the MIMEType will be retrieved from the database</param>
        private void OpenBlob(string tableName, string dataColumnName, string openProcedure, int identity, string fileNameColumnName, string MIMETypeColumnName)
        {
            SqlCommand openSqlCommand = _connection.CreateCommand();

            if (openProcedure != null && openProcedure.Length > 0) //We should use a stored procedure
            {
                openSqlCommand.CommandType = CommandType.StoredProcedure;
                openSqlCommand.CommandText = openProcedure;
            }
            else //We should generate SQL Queries
            {
                if (openProcedure != null) openProcedure = null; //Make sure the openProcedure parameter is null so future tests are simpler

                openSqlCommand.CommandText = string.Format("SELECT @Pointer = TEXTPTR([{0}]), @Size=datalength([{0}]){2}{3} FROM [{1}] WHERE $IDENTITY = @Identity", dataColumnName, tableName,
                    fileNameColumnName != null ? ",@FileName=" + fileNameColumnName : string.Empty,
                    MIMETypeColumnName != null ? ",@MIMEType=" + MIMETypeColumnName : string.Empty);
            }
            AddWithValue(openSqlCommand.Parameters, "@Identity", identity);
            openSqlCommand.Parameters.Add("@Pointer", SqlDbType.Binary, 16).Direction = ParameterDirection.Output;
            openSqlCommand.Parameters.Add("@Size", SqlDbType.Int).Direction = ParameterDirection.Output;

            if (openProcedure != null || fileNameColumnName != null) openSqlCommand.Parameters.Add("@FileName", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;
            if (openProcedure != null || MIMETypeColumnName != null) openSqlCommand.Parameters.Add("@MIMEType", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

            
            //Try to open the blob
            _connection.Open();
            try { openSqlCommand.ExecuteNonQuery(); }
            finally { _connection.Close(); }

            //Store pointer and length
            _pointer = (byte[])openSqlCommand.Parameters["@Pointer"].Value;
            _length = (int)openSqlCommand.Parameters["@Size"].Value;

            if (openProcedure != null || fileNameColumnName != null) _fileName = (string)openSqlCommand.Parameters["@FileName"].Value;
            if (openProcedure != null || MIMETypeColumnName != null) _MIMEType = (string)openSqlCommand.Parameters["@MIMEType"].Value;

            //Start reading at the beginning
            _offset = 0;

            //Stream is now officially open
            _isOpen = true;
        }

        /// <summary>
        /// Creates a new row in the database for read/write access
        /// </summary>
        /// <param name="tableName">Name of table in which to insert data</param>
        /// <param name="dataColumnName">Name of column in which to insert data</param>
        /// <param name="partialFlagColumnName"><c>Optional</c> If specified, this column will be set to 1 during upload and then to 0 when the upload is complete</param>
        /// <param name="fileNameColumnName"><c>Optional</c> If specified, this column will be set to file specified file name</param>
        /// <param name="fileName"><c>Optional</c> name of file</param>
        /// <param name="MIMETypeColumnName"><c>Optional</c> If specified, this column will be set to the MIME type of the created file</param>
        /// <param name="MIMEType"><c>Optional</c> MIME type of file</param>
        private void CreateBlob(string tableName, string dataColumnName, string partialFlagColumnName, string fileNameColumnName, string fileName, string MIMETypeColumnName, string MIMEType)
        {
            SqlCommand createCommand = _connection.CreateCommand();

            //Builds a SQL query that creates a row in the table specified and gets a pointer to the data field
            StringBuilder sql = new StringBuilder("SET NOCOUNT ON;");
            sql.AppendFormat("INSERT INTO [{0}] ([{1}]", tableName, dataColumnName);
            if (partialFlagColumnName != null) sql.AppendFormat(",[{0}]", partialFlagColumnName);
            if (fileNameColumnName != null) sql.AppendFormat(",[{0}]", fileNameColumnName);
            if (MIMETypeColumnName != null) sql.AppendFormat(",[{0}]", MIMETypeColumnName);
            sql.Append(") VALUES (@Bytes");
            if (partialFlagColumnName != null) sql.Append(",1");

            if (fileNameColumnName != null)
            {
                sql.Append(",@FileName");
                AddWithValue(createCommand.Parameters, "@FileName", fileName);
            }
            if (MIMETypeColumnName != null)
            {
                sql.Append(",@MIMEType");
                AddWithValue(createCommand.Parameters, "@MIMEType", MIMEType);
            }

            // Add zero bytes to this new blob
            createCommand.Parameters.Add("@Bytes", SqlDbType.VarBinary);
            createCommand.Parameters["@Bytes"].Value = new byte[0];
            createCommand.Parameters["@Bytes"].Size = 0;
            createCommand.Parameters["@Bytes"].Offset = 0;

            sql.AppendFormat(");SELECT @Identity = SCOPE_IDENTITY(); SELECT @Pointer = TEXTPTR([{0}]) FROM [{1}] WHERE $IDENTITY = @Identity", dataColumnName, tableName);

            createCommand.CommandText = sql.ToString();

            createCommand.Parameters.Add("@Pointer", SqlDbType.Binary, 16).Direction = ParameterDirection.Output;
            createCommand.Parameters.Add("@Identity", SqlDbType.Int).Direction = ParameterDirection.Output;

            //Create the table row for the image data
            _connection.Open();
            try { createCommand.ExecuteNonQuery(); }
            finally { _connection.Close(); }

            //Get pointer and identity
            _pointer = (byte[])createCommand.Parameters["@Pointer"].Value;
            _identity = (int)createCommand.Parameters["@Identity"].Value;
            _offset = 0;
        }



        private void GenerateReadCommand(string tableName, string dataColumnName, string readProcedure)
        {
            if ((readProcedure == null || readProcedure.Length == 0) && (tableName == null || tableName.Length == 0 || dataColumnName == null || dataColumnName.Length == 0))
                _readCommand = null;
            else
            {
                _readCommand = _connection.CreateCommand();
                if (readProcedure != null && readProcedure.Length > 0) {
                    _readCommand.CommandType = CommandType.StoredProcedure;
                    _readCommand.CommandText = readProcedure;
                } else
                    _readCommand.CommandText = string.Format("READTEXT [{0}].[{1}] @Pointer @Offset @Size", tableName, dataColumnName);

                //@Identity is not used by the created code, but may be used by the procedure (if they choose to go with the "SUBSTRING"-approach)
                AddWithValue(_readCommand.Parameters, "@Identity", _identity);

                //Add required parameters
                _readCommand.Parameters.Add("@Pointer", SqlDbType.Binary, 16).Value = _pointer;
                _readCommand.Parameters.Add("@Offset", SqlDbType.Int);
                _readCommand.Parameters.Add("@Size", SqlDbType.Int);
            }
        }

        private void GenerateDeleteCommand(string tableName, string deleteProcedure)
        {
            if ((tableName == null || tableName.Length == 0) && ((deleteProcedure == null || deleteProcedure.Length == 0)))
                _deleteCommand = null;
            else
            {
                _deleteCommand = _connection.CreateCommand();
                if (deleteProcedure != null && deleteProcedure.Length > 0) {
                    _deleteCommand.CommandType = CommandType.StoredProcedure;
                    _deleteCommand.CommandText = deleteProcedure;
                } else
                    _deleteCommand.CommandText = string.Format("DELETE FROM [{0}] WHERE $IDENTITY = @Identity", tableName);
                AddWithValue(_deleteCommand.Parameters, "@Identity", _identity);
            }
        }

        private void GenerateWriteCommand(string tableName, string dataColumnName, string writeProcedure)
        {
            //Check if we have the data wee need to create a write command
            if ((tableName == null || tableName.Length == 0 || dataColumnName == null || dataColumnName.Length == 0) && ((writeProcedure == null || writeProcedure.Length == 0)))
                _writeCommand = null;
            else
            {
                _writeCommand = _connection.CreateCommand();
                if (writeProcedure != null && writeProcedure.Length > 0)
                {
                    _writeCommand.CommandType = CommandType.StoredProcedure;
                    _writeCommand.CommandText = writeProcedure;
                }
                else
                    _writeCommand.CommandText = string.Format("UPDATETEXT [{0}].[{1}] @Pointer @Offset @Delete WITH LOG @Bytes", tableName, dataColumnName);

                //@Identity is not used by the created code, but may be used by the procedure (if they choose to go with the ".WRITE"-approach)
                AddWithValue(_writeCommand.Parameters, "@Identity", _identity);

                //Add required parameters
                _writeCommand.Parameters.Add("@Pointer", SqlDbType.Binary, 16).Value = _pointer;
                _writeCommand.Parameters.Add("@Offset", SqlDbType.Int).Value = _offset;
                _writeCommand.Parameters.Add("@Delete", SqlDbType.Int).Value = 0; // delete inserted 0x0 character
                _writeCommand.Parameters.Add("@Bytes", SqlDbType.VarBinary);
            }
        }

        private void GenerateCleanupCommand(string tableName, string dataColumnName, string partialFlagColumnName, string cleanupProcedure)
        {
            //Check if we need a cleanup-command
            if (partialFlagColumnName == null && cleanupProcedure == null)
                _cleanupCommand = null;
            else
            {
                _cleanupCommand = _connection.CreateCommand();

                if (cleanupProcedure != null && cleanupProcedure.Length > 0)
                {
                    _cleanupCommand.CommandType = CommandType.StoredProcedure;
                    _cleanupCommand.CommandText = cleanupProcedure;
                }
                else
                    _cleanupCommand.CommandText = string.Format("UPDATE [{0}] SET [{1}]=0 WHERE $Identity=@Identity", tableName, partialFlagColumnName, _identity);

                AddWithValue(_cleanupCommand.Parameters, "@Identity", _identity);
            }
        }


        /// <summary>
        /// Simple constructor mainly for reading, using generated SQL queries
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="dataColumnName"></param>
        /// <param name="identity"></param>
        /// <param name="fileNameColumnName"></param>
        /// <param name="MIMETypeColumnName"></param>
        /// <param name="access"></param>
        public SqlServerBlobStream(string connectionString, string tableName, string dataColumnName, int identity, string fileNameColumnName, string MIMETypeColumnName, FileAccess access)
        {            
            _identity = identity;
            _connection = new SqlConnection(connectionString);
            OpenBlob(tableName, dataColumnName, null, identity, fileNameColumnName, MIMETypeColumnName);
            GenerateReadCommand(tableName, dataColumnName, null);
        }

        /// <summary>
        /// Simple constructor mainly for reading, using stored procedures in the database
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="identity"></param>
        /// <param name="openProcedure"></param>
        /// <param name="readProcedure"></param>
        /// <param name="access"></param>
        public SqlServerBlobStream(string connectionString, int identity, string openProcedure, string readProcedure, FileAccess access)
        {
            _identity = identity;
            _connection = new SqlConnection(connectionString);
            OpenBlob(null, null, openProcedure, identity, null, null);
            GenerateReadCommand(null, null, readProcedure);
        }


        /// <summary>
        /// Initializor; creates a SqlServerBlobStream agains given database with given settings
        /// </summary>
        /// <param name="ConnectionString">Connection string agains the database to use</param>
        /// <param name="ConnectionName">Name of ConnectionString from web.config to use to cennct to database</param>
        /// <param name="CreatorProcedure"><c>Optional</c> Procedure to use when creating the file-containing row in the database</param>
        /// <param name="TableName">Name of table to which we write the streamed data</param>
        /// <param name="DataColumnName">Name of table column to store data into (usually of type <c>Text</c> or <c>Image</c>)</param>
        /// <param name="PartialFlagColumnName"><c>Optional</c> Name of table column to store a "partial"-flag in; while uploading this will be set to 1 (or set by CreatorProcedure), when done it will be set to 0</param>
        /// <param name="FileNameColumnName"><c>Optional</c> Name of table column where the name of the uploaded file will be stored</param>
        /// <param name="FileName"><c>Optional</c> Name of the uploaded file</param>
        /// <param name="MIMETypeColumnName"><c>Optional</c> Name of table column where the MIME-type of the uploaded file will be stored</param>
        /// <param name="MIMEType"><c>Optional</c> MIME-type of the uploaded file</param>
        private void Initialize(string connectionString, string tableName, string dataColumnName, string partialFlagColumnName, string fileNameColumnName, string fileName, string mimeTypeColumnName, string mimeType,
            string createProcedure, string openProcedure, string writeProcedure, string readProcedure, string cleanupProcedure, string renameProcedure, string storeHashProcedure, string deleteProcedure)
        {
            _fileName = fileName;
            _MIMEType = mimeType;

            _connection = new SqlConnection(connectionString);

            if (createProcedure != null && createProcedure.Length > 0)
            {
                SqlCommand createCommand = _connection.CreateCommand();
                createCommand.CommandType = System.Data.CommandType.StoredProcedure;
                createCommand.CommandText = createProcedure;

                //Assume the procedure accepts the parameters "FileName" and "MIMEType"
                AddWithValue(createCommand.Parameters, "@FileName", fileName);
                AddWithValue(createCommand.Parameters, "@MIMEType", mimeType);



                SqlParameter PointerOutParam = createCommand.Parameters.Add("@Pointer", SqlDbType.Binary, 16);
                PointerOutParam.Direction = ParameterDirection.Output;
                SqlParameter IdentityOutParam = createCommand.Parameters.Add("@Identity", SqlDbType.Int);
                IdentityOutParam.Direction = ParameterDirection.Output;

                //Create the table row for the image data
                _connection.Open();
                try { createCommand.ExecuteNonQuery(); }
                finally { _connection.Close(); }

                _pointer = (byte[])PointerOutParam.Value;
                _identity = (int)IdentityOutParam.Value;
                _offset = 0;
            }
            else
                CreateBlob(tableName, dataColumnName, partialFlagColumnName, fileNameColumnName, fileName, mimeTypeColumnName, mimeType);
      

            GenerateCleanupCommand(tableName, dataColumnName, partialFlagColumnName, cleanupProcedure);
            GenerateDeleteCommand(tableName, deleteProcedure);
            GenerateWriteCommand(tableName, dataColumnName, writeProcedure);

            _isOpen = true;
        }


        /// <summary>Flush is not implemented since all data is sent through immediatly</summary>
        public override void Flush() { }

        private long _length = 0;
        public override long Length { get { return _length; } }

        /// <summary>Current filepointer position in file</summary>
        public override long Position
        {
            get { return _offset; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        /// <summary>
        /// Moves filepointer to given location in file
        /// </summary>
        /// <param name="offset">New position of file pointer relative to the origin</param>
        /// <param name="origin">Origin to seek from</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) 
        {
            switch (origin)
            {
                case SeekOrigin.Begin: _offset = offset; break;
                case SeekOrigin.Current: _offset += offset; break;
                case SeekOrigin.End: _offset = this.Length + offset; break;
            }
            if (_offset < 0) _offset = 0;
            if (_offset > this.Length) _offset = this.Length;
            return _offset;
        }

        public override void SetLength(long value)
        {
            //if (!_isOpen) return new ObjectDisposedException("SqlServerBlobStream");
            throw new NotSupportedException("The method or operation 'SqlServerBlobStream.SetLength' is not implemented.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //Implement standard stream behaviour
            if (!_isOpen) throw new ObjectDisposedException("SqlServerBlobStream");
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            if (buffer.Length < offset + count) throw new ArgumentException("offset + count does not fit into buffer");

            //Make sure count does not take us outside the blob
            if (_offset + count > _length) 
                count = (int)(_length - _offset);  //Instead just read to the end

            if (count == 0) return 0;   //No bytes to read...
            
            try
            {
                /*
                _readCommand.Parameters["@Bytes"].Value = buffer;
                _readCommand.Parameters["@Bytes"].Size = count;
                _readCommand.Parameters["@Bytes"].Offset = offset;
                */
                _readCommand.Parameters["@Offset"].Value = _offset;
                _readCommand.Parameters["@Size"].Value = count;

                //Local buffer.
                //TODO: See if I can skip this somehow... hopefully the sqlclient namespace has some support for writing to existing buffers
                byte[] data;

                _connection.Open();
                //try { _readCommand.ExecuteNonQuery(); }
                try { data = (byte[])_readCommand.ExecuteScalar(); }
                finally { _connection.Close(); }


                //Just to make sure we got the right amount of bytes
                count = data.Length;
                
                System.Buffer.BlockCopy(data, 0, buffer, offset, count);

                _offset += count;
                return count;

            }
            catch (Exception ex)
            {
                throw new IOException("IO failed in SqlServerBlobStream.Read", ex);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //Make sure stream is open
            if (!_isOpen) throw new ObjectDisposedException("SqlServerBlobStream");
            //Test standard stream test
            if (buffer.Length < offset + count) throw new ArgumentException("offset + count does not fit into buffer");
            if (buffer == null) throw new ArgumentNullException("buffer");


            //Write to database
            if (count == 0) return;
            try
            {
                _writeCommand.Parameters["@Bytes"].Value = buffer;
                _writeCommand.Parameters["@Bytes"].Size = count;
                _writeCommand.Parameters["@Bytes"].Offset = offset;

                _connection.Open();
                try { _writeCommand.ExecuteNonQuery(); }
                finally { _connection.Close(); }

                //Mark this stream as modified
                _modified = true;

                _writeCommand.Parameters["@Delete"].Value = 0; //Not sure if this is really neccessary...
                _offset += count;
                _writeCommand.Parameters["@Offset"].Value = _offset;
            }
            catch (Exception ex)
            {
                throw new IOException("Sql IO failed in SqlServerBlobStream.Write", ex);
            }

            //Update length if this operation made the blob larger
            _length = Math.Max(_offset, _length);
        }

        /// <summary>
        /// Closes the current stream and releases any resources
        /// </summary>
        public override void Close()
        {
            if (_isOpen)
            {
                //If we have a cleanup-command run it now
                if (_modified &&_cleanupCommand!=null)
                {
                    _connection.Open();
                    try { _cleanupCommand.ExecuteNonQuery(); }
                    finally { _connection.Close(); }
                }
                _isOpen = false;
            }
        }

        /// <summary>
        /// Reopens the stream if it has been closed
        /// </summary>
        public void ReOpen()
        {
            if (_disposed) throw new ObjectDisposedException("SqlServerBlobStream");
            _isOpen = true;
        }

        /// <summary>
        /// Frees all resources used by stream
        /// </summary>
        public new void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Close();

                if (_writeCommand != null) _writeCommand.Dispose();
                if (_readCommand != null) _readCommand.Dispose();
                if (_cleanupCommand != null) _cleanupCommand.Dispose();
                if (_deleteCommand != null) _deleteCommand.Dispose();

                _connection.Dispose();
            }
        }

        /// <summary>
        /// Deletes the table row containing the current stream from the database
        /// </summary>
        public void Delete()
        {
            if (_deleteCommand == null) throw new Exception("Not configured to delete posts");
            _connection.Open();
            try { _deleteCommand.ExecuteNonQuery(); }
            finally { _connection.Close(); }
            _isOpen = false;
        }
        
        internal static SqlParameter AddWithValue(SqlParameterCollection parameters, string paramName, int paramValue)
        {
#if NETv2_0
            return parameters.AddWithValue(paramName, paramValue);
#else
            return parameters.Add(paramName, paramValue);
#endif
        }
        
        internal static SqlParameter AddWithValue(SqlParameterCollection parameters, string paramName, string paramValue)
        {
#if NETv2_0
            return parameters.AddWithValue(paramName, paramValue);
#else
            return parameters.Add(paramName, paramValue);
#endif
        }
                                                         
    }
}
