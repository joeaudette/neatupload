using System;
using System.IO;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;
using Hitone.Web.SqlServerUploader;

namespace UploaderTest
{
    /// <summary>
    /// This page demostrates how to use the SqlServerBlobStream to stream data from the database directly to the client browser
    /// </summary>
    public class DBRead : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //Make sure there is an 'id' in the query string
            if (Request.QueryString["id"] == null) throw new ArgumentException("No file id was specified");

            //Make sure there is no crap in the HTTP headers that would be sent
            Response.ClearHeaders();

            //Get ID of file to load form the database
            int id = int.Parse(Request.QueryString["id"]);

            SqlServerUploadStorageProvider provider = (SqlServerUploadStorageProvider)UploadStorage.Provider;

            // Use the provider attributes to connect to the database and use stored procs or generated sql
            // to get a stream on the file.
            SqlServerBlobStream blob = null;
            if (provider.OpenProcedure != null && provider.ReadProcedure != null)
            {
                blob = new SqlServerBlobStream(
                    provider.ConnectionString, id, provider.OpenProcedure, provider.ReadProcedure, FileAccess.Read);
            }
            else
            {
                blob = new SqlServerBlobStream(
                    provider.ConnectionString, provider.TableName, provider.DataColumnName, id,
                    provider.FileNameColumnName, provider.MIMETypeColumnName, FileAccess.Read);
            }

            // Set the filename and MIME-type of the response to that given by the file
            Response.ContentType = blob.MIMEType;
            Response.AddHeader("Content-disposition", "attachment;filename=\""+blob.FileName+"\"");

            //Pipe the file data to the browser
            DataPipe.Pipe(blob, Response.OutputStream);
            
            // Close the stream from the DB.
            blob.Close();

            //Finished!
            Response.End();
        }
    }


    /// <summary>
    /// Helper class to stream data from one stream to another using a 100kb buffer
    /// </summary>
    public class DataPipe
    {
        /// <summary> Default buffer size </summary>
        public const int DefaultBufferSize = 1024 * 100;

        /// <summary>
        /// Pipes data from one stream to another using the default buffer size
        /// </summary>
        /// <param name="input">Stream to read from</param>
        /// <param name="output">Stream to write to</param>
        /// <returns>Number of bytes piped</returns>
        public static int Pipe(Stream input, Stream output) { return Pipe(input, output, DefaultBufferSize); }

        /// <summary>
        /// Pipes data from one stream to another
        /// </summary>
        /// <param name="input">Stream to read from</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="MaxBufferSize">Size of buffer to use</param>
        /// <returns>Number of bytes piped</returns>
        public static int Pipe(Stream input, Stream output, int MaxBufferSize)
        {
            //Internal buffer are two buffers, each half of allowed buffer size, aligned to 1kb blocks
            int bufferSize = (MaxBufferSize / 2) & ~1023;
            if (bufferSize <= 0) throw new Exception("Specified buffer size to small");

            byte[][] buffer = new byte[2][];
            buffer[0] = new byte[bufferSize];
            buffer[1] = new byte[bufferSize];
            int currentBuffer = 0;

            int r, total=0;
            IAsyncResult asyncRead,asyncWrite;

            //Read first block
            r = input.Read(buffer[currentBuffer], 0, bufferSize);

            //Continue while we're getting data
            while (r > 0)
            {
                //read and write simultaneously
                asyncWrite = output.BeginWrite(buffer[currentBuffer], 0, r, null, null);
                asyncRead = input.BeginRead(buffer[1-currentBuffer], 0, bufferSize, null, null);
                //Wait for both
                output.EndWrite(asyncWrite);
                r = input.EndRead(asyncRead);
                //Switch buffers
                currentBuffer = 1 - currentBuffer;
                //Count bytes
                total += r;
            }

            //Return number of bytes piped
            return total;
        }


    }
}
