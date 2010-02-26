

SQL Server Uploader
=============================

This file contains some notes about how this plug-in to NeatUpload works. To understand this plug-in you probably should have tried NeatUpload first.
This plug-in can either generate SQL itself and use against the server or use stored procedures on the server for all tasks. An example of how those stored procedures
could look is supplied at the end of this document. 

This plugin is written by Joakim Wennergren (jokedst@gmail.com). Feel free to contact me with any questions, or post them at the NeatUpload forum (www.bretle.com).

This plugin is release under the GNU Lesser General Public License. See the disclaimer in the source code files for more information.


===============
USAGE
===============
To specify whether this plug-in should generate SQL queries or use stored procedures, you configure the "provider" in web.config. If you specify procedure names 
(createProcedure, openProcedure, readProcedure, writeProcedure, deleteProcedure, cleanupProcedure, renameProcedure, storeHashProcedure) the code uses the procedures.
If you instead specify table and column names (tableName, dataColumnName, partialFlagColumnName, fileNameColumnName, mimeTypeColumnName, hashColumnName) the code 
will build its own SQL Queries. You can also combine both methods, if procedures are named they have precedence.

There is also the setting "hashAlgorithm". If specified, the uploader will calculate a hash value of the uploaded file while it is uploaded. The code is mainly
copied from Dean's HashingFilesystemUploadStorageProvider plug-in. The Hash algorithm may be any supported by the .NET framework, including the common MD5, SHA1 and SHA256.

Note that to specify connectionstring this plug-in supports using the ConectionStrings-section in web.config under .NET v2.0. To use a named connectionstring, enter the 
name of the connection string instead of the string itself (see included web.config).

The examples in this project use a database table that looks like this:

CREATE TABLE [FileTable] (
	[Id] [int] IDENTITY (1, 1) PRIMARY KEY NOT NULL ,
	[FileName] [nvarchar] (50) NULL,
	[DataField] [image] NOT NULL ,
	[Partial] [tinyint] NOT NULL ,
	[MimeType] [nvarchar] (50) NULL ,
	[Created] [datetime] NOT NULL CONSTRAINT [DF_FileTable_Created] DEFAULT (getdate()),
	[FileHash] [nvarchar] (50) NULL ,
)

The only necessary fields for the uploader to work are an IDENTITY field and a Image/varbinary field for the data, the others are optional.
Change the connection strings in the web.config to point to your database and the right table


Note that the version that uses generated SQL (as opposed to stored procedures) only works on SQL 2005 because it makes heavy use of the "$IDENTITY" object
to reference the IDENTITY column of the table. To fix this I should add a "IdentityColumnName"-setting, but since I only use SQL 2005 (and stored procedures)
it has never been an issue... Use stored procedures to get around this problem if you don't want to rewrite these classes and must use SQL Server 2000.




When using NeatUpload you get a bunch of warnings about the web.config file since the NeatUpload-addons to the system.web section isn't in Microsoft's .xsd-schema for
web.config. This is nothing to worry about, but if you find the warnings annoying, add this to C:\Program Files\Microsoft Visual Studio 8\Xml\Schemas\DotNetConfig.xsd 
to get rid of them. It should be placed in the "choice" section under the element "configuration/system.web"


				<xs:element name="neatUpload" vs:help="configuration/system.web/neatUpload">
					<xs:complexType>
						<xs:choice minOccurs="0" maxOccurs="1">
							<xs:element name="providers" vs:help="configuration/system.web/neatUpload/providers">
								<xs:complexType>
									<xs:choice minOccurs="0" maxOccurs="unbounded">
										<xs:element name="add" vs:help="configuration/system.web/httpHandlers/add">
											<xs:complexType>
												<xs:attribute name="name" type="xs:string" use="required" />
												<xs:attribute name="type" type="xs:string" use="required" />
												<xs:attribute name="tempDirectory" type="xs:string" use="optional" />
												<!-- HashingFilesystemUploadStorageProvider specific -->
												<xs:attribute name="algorithm" type="xs:string" use="optional" />
												<!-- SqlServerUploader specific -->
												<xs:attribute name="connectionString" type="xs:string" use="optional" />
												<xs:attribute name="connectionName" type="xs:string" use="optional" />
												<xs:attribute name="tableName" type="xs:string" use="optional" />
												<xs:attribute name="dataColumnName" type="xs:string" use="optional" />
												<xs:attribute name="partialFlagColumnName" type="xs:string" use="optional" />
												<xs:attribute name="fileNameColumnName" type="xs:string" use="optional" />
												<xs:attribute name="mimeTypeColumnName" type="xs:string" use="optional" />
												<xs:attribute name="hashAlgorithm" type="xs:string" use="optional" />
												<xs:attribute name="hashColumnName" type="xs:string" use="optional" />	
												<xs:attribute name="createProcedure" type="xs:string" use="optional" />
												<xs:attribute name="openProcedure" type="xs:string" use="optional" />
												<xs:attribute name="writeProcedure" type="xs:string" use="optional" />
												<xs:attribute name="readProcedure" type="xs:string" use="optional" />
												<xs:attribute name="cleanupProcedure" type="xs:string" use="optional" />
												<xs:attribute name="renameProcedure" type="xs:string" use="optional" />
												<xs:attribute name="storeHashProcedure" type="xs:string" use="optional" />
												<xs:attribute name="deleteProcedure" type="xs:string" use="optional" />											
											</xs:complexType>
										</xs:element>										
									</xs:choice>
								</xs:complexType>
							</xs:element>
						</xs:choice>
						<xs:attribute name="defaultProvider" type="xs:string" use="optional" />
						<xs:attribute name="useHttpModule" type="small_boolean_Type" use="optional" />
						<xs:attribute name="maxNormalRequestLength" type="xs:int" use="optional" />
						<xs:attribute name="maxRequestLength" type="xs:int" use="optional" />
						<xs:attribute name="postBackIDQueryParam" type="xs:string" use="optional" />						
					</xs:complexType>
				</xs:element>



=====================================================
Stored Procedures example
SQL Server 2005:
=====================================================

CREATE TABLE [FileTable] (
	[Id] [int] IDENTITY (1, 1) PRIMARY KEY NOT NULL ,
	[FileName] [nvarchar] (50) NOT NULL ,
	[DataField] [image] NOT NULL ,
	[Partial] [tinyint] NOT NULL ,
	[MimeType] [nvarchar] (50) NOT NULL ,
	[Created] [datetime] NOT NULL CONSTRAINT [DF_FileTable_Created] DEFAULT (getdate()),
	[FileHash] [nvarchar] (50) NULL ,
)

GO

CREATE Procedure CreateBlob
	@Identity Numeric Output,
	@Pointer Binary(16) Output,
	@FileName VarChar(250) = null,
	@MIMEType VarChar(250) = null
As Begin Set NoCount ON;
	Insert Into VerboseTable2 (Datafield,FileName,MimeType,Partial) Values ('',@FileName,@MimeType,1)
	Select @Identity = SCOPE_IDENTITY()
	Select @Pointer = TEXTPTR(DataField) From VerboseTable2 Where $IDENTITY = @Identity
End

Go

CREATE Procedure OpenBlob
	@Identity Numeric,
	@Pointer VarBinary(max) Output,
	@Size Int Output,
	@FileName VarChar(250) Output,
	@MIMEType VarChar(250) Output
As Begin Set NoCount On
	Select	@Pointer = TEXTPTR(DataField), 
			@Size = DATALENGTH(DataField),
			@FileName = [FileName],
			@MIMEType = MIMEType
	From VerboseTable2 Where $IDENTITY = @Identity
End

Go

CREATE Procedure ReadBlob
	@Identity Numeric, --ignored in this implementation, here for reference
	@Pointer Binary(16),
	@Offset Int,
	@Size Int
As Begin Set NoCount On
	ReadText VerboseTable2.DataField @Pointer @Offset @Size
End

Go

CREATE Procedure WriteBlob
	@Identity Numeric, --ignored in this implementation, here for reference
	@Pointer Binary(16),
	@Bytes VarBinary(max),
	@Offset Int,
	@Delete Int
As Begin Set NoCount On
	UpdateText VerboseTable2.DataField @Pointer @Offset @Delete With Log @Bytes
End

Go

CREATE Procedure CleanUpBlob
	@Identity Numeric
As Begin Set NoCount On
	Update VerboseTable2 Set Partial=0 Where $Identity=@Identity
End

Go

CREATE Procedure DeleteBlob
	@Identity Numeric
As Begin Set NoCount On
	Delete From VerboseTable2 Where $Identity=@Identity
End

Go

CREATE Procedure RenameBlob
	@Identity Numeric,
	@FileName VarChar(250)
As Begin Set NoCount On
	Update VerboseTable2 Set [FileName]=@FileName Where $Identity=@Identity
End

Go

CREATE Procedure FinalizeBlob
	@Identity Numeric,
	@Hash VarChar(250)
As Begin Set NoCount On
	Update VerboseTable2 Set FileHash=@Hash Where $Identity=@Identity
End




=====================================================
SQL SERVER 2000 
(main difference is the lack of a $IDENTITY-object and the cap of 8000 bytes in a varbinary):
=====================================================

CREATE TABLE [FileTable] (
	[Id] [int] IDENTITY (1, 1) PRIMARY KEY NOT NULL ,
	[FileName] [nvarchar] (50) NOT NULL ,
	[DataField] [image] NOT NULL ,
	[Partial] [tinyint] NOT NULL ,
	[MimeType] [nvarchar] (50) NOT NULL ,
	[Created] [datetime] NOT NULL CONSTRAINT [DF_FileTable_Created] DEFAULT (getdate()),
	[FileHash] [nvarchar] (50) NULL ,
)

GO

CREATE Procedure CleanUpBlob
	@Identity Numeric
As Begin Set NoCount On
	Update FileTable Set Partial=0 Where Id=@Identity
End

GO

CREATE Procedure CreateBlob
	@Identity Numeric Output,
	@Pointer Binary(16) Output,
	@FileName VarChar(250) = null,
	@MIMEType VarChar(250) = null
As Begin Set NoCount ON;
	Insert Into FileTable (Datafield,FileName,MimeType,Partial) Values ('',@FileName,@MimeType,1)
	Select @Identity = SCOPE_IDENTITY()
	Select @Pointer = TEXTPTR(DataField) From FileTable Where id = @Identity
End

GO

CREATE Procedure DeleteBlob
	@Identity Numeric
As Begin Set NoCount On
	Delete From FileTable Where id=@Identity
End

GO

CREATE Procedure FinalizeBlob
	@Identity Numeric,
	@Hash VarChar(250)
As Begin Set NoCount On
	Update FileTable Set FileHash=@Hash Where id=@Identity
End

GO

CREATE Procedure OpenBlob
	@Identity Numeric,
	@Pointer VarBinary(8000) Output,
	@Size Int Output,
	@FileName VarChar(250) Output,
	@MIMEType VarChar(250) Output
As Begin Set NoCount On
	Select	@Pointer = TEXTPTR(DataField), 
			@Size = DATALENGTH(DataField),
			@FileName = [FileName],
			@MIMEType = MIMEType
	From FileTable Where id = @Identity
End

GO

CREATE Procedure ReadBlob
	@Identity Numeric, --ignored in this implementation, here for reference
	@Pointer Binary(16),
	@Offset Int,
	@Size Int
As Begin Set NoCount On
	ReadText FileTable.DataField @Pointer @Offset @Size
End

GO

CREATE Procedure RenameBlob
	@Identity Numeric,
	@FileName VarChar(250)
As Begin Set NoCount On
	Update FileTable Set [FileName]=@FileName Where id=@Identity
End

GO

CREATE Procedure WriteBlob
	@Identity Numeric, --ignored in this implementation, here for reference
	@Pointer Binary(16),
	@Bytes VarBinary(8000),
	@Offset Int,
	@Delete Int
As Begin Set NoCount On
	UpdateText FileTable.DataField @Pointer @Offset @Delete With Log @Bytes
End

