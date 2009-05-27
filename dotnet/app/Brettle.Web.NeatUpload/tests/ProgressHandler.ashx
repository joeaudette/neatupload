<%@ WebHandler Language="C#" Class="ProgressHandler" %>

using System;
using System.Web;
using Brettle.Web.NeatUpload;

public class ProgressHandler : IHttpHandler, IUploadProgressState
{
	public void ProcessRequest (HttpContext context) {
		string postBackID = context.Request.Params["postBackID"];
		UploadModule.BindProgressState(postBackID, null, this);
		string message = "";
		if (Status == UploadStatus.Rejected)
			message = Rejection.Message;
		else if (Status == UploadStatus.Failed)
			message = Failure.Message;
		context.Response.ContentType = "application/json";

		double percentComplete = Math.Floor(100 * FractionComplete);
		string processingStateJson = "null";
		ProgressInfo progressInfo = ProcessingState as ProgressInfo;
		if (progressInfo != null)
		{
			percentComplete = Math.Floor(100.0 * progressInfo.Value / progressInfo.Maximum);
			processingStateJson = String.Format(@"{{ ""Value"" : {0}, ""Maximum"" : {1}, ""Units"" : {2}, ""Text"" : ""{3}"" }}",
				progressInfo.Value, progressInfo.Maximum, progressInfo.Units, progressInfo.Text != null ? progressInfo.Text : "");
		}
		
		string jsonFormat = @"{{ 
  ""Status"" : ""{0}"",
  ""BytesRead"" : {1},
  ""BytesTotal"" : {2},
  ""PercentComplete"" : {3},
  ""BytesPerSec"" : {4},
  ""Message"" : ""{5}"",
  ""SecsRemaining"" : {6},
  ""SecsElapsed"" : {7},
  ""CurrentFileName"" : ""{8}"",
  ""ProcessingState"" : {9}  
}}
";
		string json = String.Format(jsonFormat,
Status, BytesRead, BytesTotal, percentComplete, BytesPerSec, message,
Math.Floor(TimeRemaining.TotalSeconds), Math.Floor(TimeElapsed.TotalSeconds), CurrentFileName, processingStateJson);
		context.Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
		context.Response.Write(json);
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }


	#region IUploadProgressState Members

	private UploadStatus _Status = UploadStatus.Unknown;
	public UploadStatus Status
	{
		get { return _Status; }
		set { _Status = value; }
	}

	private long _BytesRead;
	public long BytesRead
	{
		get { return _BytesRead; }
		set { _BytesRead = value; }
	}

	private long _FileBytesRead;
	public long FileBytesRead
	{
		get { return _FileBytesRead; }
		set { _FileBytesRead = value; }
	}

	private long _BytesTotal;
	public long BytesTotal
	{
		get { return _BytesTotal; }
		set { _BytesTotal = value; }
	}

	private double _FractionComplete;
	public double FractionComplete
	{
		get { return _FractionComplete; }
		set { _FractionComplete = value; }
	}

	private int _BytesPerSec;
	public int BytesPerSec
	{
		get { return _BytesPerSec; }
		set { _BytesPerSec = value; }
	}

	private UploadException _Rejection;
	public UploadException Rejection
	{
		get { return _Rejection; }
		set { _Rejection = value; }
	}

	private Exception _Failure;
	public Exception Failure
	{
		get { return _Failure; }
		set { _Failure = value; }
	}

	private TimeSpan _TimeRemaining;
	public TimeSpan TimeRemaining
	{
		get { return _TimeRemaining; }
		set { _TimeRemaining = value; }
	}

	private TimeSpan _TimeElapsed;
	public TimeSpan TimeElapsed
	{
		get { return _TimeElapsed; }
		set { _TimeElapsed = value; }
	}

	private string _CurrentFileName;
	public string CurrentFileName
	{
		get { return _CurrentFileName; }
		set { _CurrentFileName = value; }
	}

	private UploadedFileCollection _Files;
	public UploadedFileCollection Files
	{
		get { return _Files; }
		set { _Files = value; }
	}

	private object _ProcessingState;
	public object ProcessingState
	{
		get { return _ProcessingState; }
		set { _ProcessingState = value; }
	}

	#endregion
}