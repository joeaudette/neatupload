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

// Define this if you are compiling under .NET v2.0. If not defined turns of 2.0 specific features
// #define NETv2_0

using System;
using System.Text;
using System.Collections.Specialized;
using Brettle.Web.NeatUpload;

namespace Hitone.Web.SqlServerUploader
{
    /// <summary>
    /// Storage provider for NeatUpload that allows streaming uploaded files directly into a database
    /// </summary>
    public class SqlServerUploadStorageProvider : UploadStorageProvider
    {
        //Private variables exposed by parameters below (see parameters for description)
        private string _name = null;
        private string _connectionString = null;
        private string _connectionName = null;

        private string _createProcedure = null;
        private string _openProcedure = null;
        private string _writeProcedure = null;
        private string _readProcedure = null;
        private string _cleanupProcedure = null;
        private string _renameProcedure = null;
        private string _storeHashProcedure = null;
        private string _deleteProcedure = null;

        private string _tableName = null;
        private string _dataColumnName = null;
        private string _partialFlagColumnName = null;
        private string _fileNameColumnName = null;
        private string _MIMETypeColumnName = null;
        private string _hashColumnName = null;
        private string _hashAlgorithm = null;
        
    //Inherited behaviour related
        /// <summary> Unique friendly name for this provider </summary>
        public override string Name { get { return _name; } }


    //Connection related
        /// <summary> Connection string to use when connecting to the database </summary>
        public string ConnectionString { get { return _connectionString; } }
        /// <summary> Name of connection string to use when connecting to the database (from then &lt;connectionStrings&gt; section in web.config)</summary>
        public string ConnectionName { get { return _connectionName; } }


    //Stored Procedure related
        /// <summary> Name of procedure to call to create a new file in the database. To create new blobs either this or TableName/DataColumnName must be available </summary>
        /// <remarks> The procedure must take these parameters: (@Pointer varbinary output, @Identity numeric output, (optional) @FileName varchar, (optional) @MIMEType varchar)</remarks>
        public string CreateProcedure { get { return _createProcedure; } }
        /// <summary> Name of procedure to call to open an existing blob in the database. To read/append data either this or TableName/DataColumnName must be available </summary>
        /// <remarks> The procedure must take these parameters: (@Pointer varbinary output, @Identity numeric, (optional) @Size int output, (optional) @FileName varchar output, (optional) @MIMEType varchar output)</remarks>
        public string OpenProcedure { get { return _openProcedure; } }
        /// <summary> Name of procedure to call to append data in the database. Either this or TableName/DataColumnName must be available </summary>
        /// <remarks> The procedure must take these parameters: (@Pointer varbinary output, @Offset int, @Delete int, @Bytes varbinary, (optional) @Identity int)</remarks>
        public string WriteProcedure { get { return _writeProcedure; } }
        /// <summary> Name of procedure to call to read data from the database. Either this or TableName/DataColumnName must be available </summary>
        /// <remarks> The procedure must take these parameters: (@Pointer varbinary output, @Offset int, @Size varbinary, (optional) @Identity int)</remarks>
        public string ReadProcedure { get { return _readProcedure; } }
        /// <summary> Name of procedure to call when the file upload is completed but before the .net web page loads. 
        /// This procedure can be used to clear the "partial upload" flag if one was used.</summary>
        /// <remarks> The procedure must not take any parameters</remarks>
        public string CleanupProcedure { get { return _cleanupProcedure; } }
        /// <summary> Name of procedure to call to change the name of the file in the database. Either this or TableName/FileNameColumnName must be available </summary>
        /// <remarks> The procedure must take these parameters: (@Identity int, @FileName varchar)</remarks>
        public string RenameProcedure { get { return _renameProcedure; } }
        /// <summary> Name of procedure to call when the file upload is completed and verified to stay in the database (i.e. MoveTo has been called).
        /// This procedure can be used to store the Hash of the file if one was calculated</summary>
        /// <remarks> The procedure must take these parameters: (@Identity numeric, (optional) @Hash varchar)</remarks>
        public string StoreHashProcedure { get { return _storeHashProcedure; } }
        /// <summary> Name of procedure to call to delete a file from the database. Either this or TableName must be available to allow deletion of files </summary>
        /// <remarks> The procedure must take these parameters: (@Identity int)</remarks>
        public string DeleteProcedure { get { return _deleteProcedure; } }


    //Used when building SQL Strings internally
        /// <summary> Table to store incoming file into. This or CreatorProcedure must be specified </summary>
        public string TableName { get { return _tableName; } }

        /// <summary> Name of table column to store data into. Should be of type 'Image'. This or CreatorProcedure must be specified </summary>
        public string DataColumnName { get { return _dataColumnName; } }

        /// <summary> <c>Optional</c> Name of table column to store a "partial"-flag in; while uploading this will be set to 1, when done it will be set to 0 </summary>
        public string PartialFlagColumnName { get { return _partialFlagColumnName; } }

        /// <summary> <c>Optional</c> Name of table column where the name of the uploaded file will be stored </summary>
        public string FileNameColumnName { get { return _fileNameColumnName; } }

        /// <summary> <c>Optional</c> Name of table column where the MIME-type of the uploaded file will be stored </summary>
        public string MIMETypeColumnName { get { return _MIMETypeColumnName; } }
        
        /// <summary> <c>Optional</c> Name of hash algorithm to use if we should hash while upload </summary>
        public string HashAlgorithm { get { return _hashAlgorithm; } }

        /// <summary> <c>Optional</c> Name of table column where the hash of the uploaded file will be stored </summary>
        public string HashColumnName { get { return _hashColumnName; } }



        /// <summary>Description of this storage provider</summary>
        public override string Description { get { return "Streams uploads to a SQL Server Database"; } }


        /// <summary>
        /// Simple safeguard agains SQL insertion. This is mostly a sanity check though since who in their right mind would start an SQL insertion attack from the web.config?
        /// </summary>
        /// <param name="name">Name of SQL table/colun that is to be checked</param>
        /// <returns>The input string with "dangerous" characters escaped (at this point only ']')</returns>
        private string safeName(string name)
        {
            return name != null ? name.Replace("]", "]]") : null;
        }

        /// <summary>
        /// Initializes the internal structures from values specified in the .config files
        /// </summary>
        /// <param name="providerName">Unique name used to refer to this instance of SqlServerStorageProvider</param>
        /// <param name="attrs">Parameters stored in the .config files</param>
        public override void Initialize(string providerName, NameValueCollection attrs)
        {
            this._name = providerName;

            //Get parameters from attrs
            _connectionString = attrs["ConnectionString"];
            _tableName = safeName(attrs["TableName"]);
            _dataColumnName = safeName(attrs["DataColumnName"]);
            _partialFlagColumnName = safeName(attrs["PartialFlagColumnName"]);
            _fileNameColumnName = safeName(attrs["FileNameColumnName"]);
            _MIMETypeColumnName = safeName(attrs["MIMETypeColumnName"]);

            _createProcedure = attrs["CreateProcedure"];
            _openProcedure = attrs["OpenProcedure"];
            _writeProcedure = attrs["WriteProcedure"];
            _readProcedure = attrs["ReadProcedure"];
            _cleanupProcedure = attrs["CleanupProcedure"];
            _renameProcedure = attrs["RenameProcedure"];
            _storeHashProcedure = attrs["StoreHashProcedure"];
            _deleteProcedure = attrs["DeleteProcedure"];

            _hashAlgorithm = attrs["HashAlgorithm"];
            _hashColumnName = safeName(attrs["HashColumnName"]);


            //In .net v2.0 there is a nice ConfigurationManager and centralized ConnectionStrings. Use it if "ConnectionName" is specified
            if (System.Environment.Version.Major >= 2 && attrs["ConnectionName"] != null && attrs["ConnectionName"].Length > 0)
            {
                _connectionName = attrs["ConnectionName"];
                // Use reflection to do:
                //   _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings[_connectionName].ConnectionString;
                // so we don't need a special 2.0 version of the assembly.
                string configAssembly = "System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL";
                Type configManager = Type.GetType("System.Configuration.ConfigurationManager, " + configAssembly, true);
                System.Reflection.PropertyInfo connStringsPropInfo = configManager.GetProperty("ConnectionStrings");
                object connStringSettingCollection = connStringsPropInfo.GetGetMethod().Invoke(null, null);
                System.Reflection.BindingFlags instanceGetPropBindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public;
                object connStringSettings = Type.GetType("System.Configuration.ConnectionStringSettingsCollection, " + configAssembly)
                    .InvokeMember("", instanceGetPropBindingFlags, null, connStringSettingCollection, new object[] { _connectionName });
                _connectionString = (string)Type.GetType("System.Configuration.ConnectionStringSettings, " + configAssembly)
                    .InvokeMember("ConnectionString", instanceGetPropBindingFlags, null, connStringSettings, null);
            }

            //Make sure we have at least a connenction string, a table name and a dataColumnName. The rest is optional
            string error = string.Empty;
            if (_connectionString == null) error = "No ConnectionString specified";
            if (_createProcedure == null && (_tableName == null || _dataColumnName == null))
                error += (error.Length > 0 ? "; " : string.Empty) + "Either CreatorProcedure or TableName/DataColumnName mut be specified";            
            if (error.Length > 0) throw new System.Xml.XmlException("Missing attribute: " + error);

        }

        public override UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName, string contentType)
        {
            return this.CreateUploadedFile(context, controlUniqueID, fileName, contentType, null);
        }

        public override UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName, string contentType, UploadStorageConfig storageConfig)
        {
            return new SqlServerUploadedFile(this, controlUniqueID, fileName, contentType, storageConfig);
        }
		
    }
}
