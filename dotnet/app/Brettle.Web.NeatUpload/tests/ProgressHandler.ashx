<%@ WebHandler Language="C#" Class="ProgressHandler" %>

using System;
using System.Web;
using Brettle.Web.NeatUpload;
using System.Text.RegularExpressions;

public class ProgressHandler : IHttpHandler, IUploadProgressState
{
	public void ProcessRequest (HttpContext context) {
		string postBackID = context.Request.Params["postBackID"];
		string controlID = context.Request.Params["controlID"];
		string cancel = context.Request.Params["cancel"];

		if (cancel != null && Boolean.Parse(cancel))
			UploadModule.CancelPostBack(postBackID);
		
		UploadModule.BindProgressState(postBackID, controlID, this);
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
			processingStateJson = String.Format(@"{{ ""Value"" : {0}, ""Maximum"" : {1}, ""Units"" : ""{2}"", ""Text"" : ""{3}"" }}",
				progressInfo.Value, progressInfo.Maximum, Quote(progressInfo.Units), progressInfo.Text != null ? Quote(progressInfo.Text) : "");
		}
			
		System.Text.StringBuilder filesJson = new System.Text.StringBuilder();
		bool isFirstFile = true;
		for (int i = 0; Files != null && i < Files.Count; i++)
		{
			UploadedFile file = Files[i];
			if (Status == UploadStatus.NormalInProgress || Status == UploadStatus.ChunkedInProgress || Status == UploadStatus.ProcessingInProgress)
			{
				AppendFileJson(filesJson, file, ref isFirstFile);
			}
			else
			{
				// If the upload isn't in progress, the file might have been disposed which
				// can cause exceptions, so we only make a best effort. 
				try
				{
					AppendFileJson(filesJson, file, ref isFirstFile);
				}
				catch (Exception ex)
				{
					// File was probably disposed so ignore the error.
				}
			}
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
  ""ProcessingState"" : {9},
  ""Files"" : [{10}]  
}}
";
		string json = String.Format(jsonFormat,
Status, BytesRead, BytesTotal, percentComplete, BytesPerSec, Quote(message),
Math.Floor(TimeRemaining.TotalSeconds), Math.Floor(TimeElapsed.TotalSeconds), Quote(CurrentFileName), processingStateJson, filesJson.ToString());
		context.Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
		context.Response.Write(json);
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }
	
	private static Regex QuotedCharsRe = new Regex(@"[\u0000-\u001f\u007f-\uffff""\\]", RegexOptions.Compiled);
	private string Quote(string s)
	{
		if (s == null) return null;
		string quoted = QuotedCharsRe.Replace(s, new MatchEvaluator(ReplaceMatch));
		return quoted;
	}

	private string ReplaceMatch(Match m)
	{
		char c = m.Value.ToCharArray()[0];
		switch (c)
		{
			case '"': return @"\""";
			case '\\': return @"\\";
			case '\n': return @"\n";
			case '\r': return @"\r";
			case '\t': return @"\t";
			case '\b': return @"\b";
			case '\f': return @"\f";
			default: return String.Format(@"\u{0:X4}", (int)c);
		}
	}

	private void AppendFileJson(System.Text.StringBuilder filesJson, UploadedFile file, ref bool isFirstFile)
	{
		// We only count uploaded files with non-empty names.  We don't use file.IsUploaded here because
		// it can return false if the file was uploaded and has since been disposed.
		if (file.FileName != null && file.FileName.Length > 0)
		{
			// If file was already disposed, ContentLength might be unavailable.  If so, use -1.
			long contentLength = -1;
			try { contentLength = file.ContentLength; }	catch (Exception) { }

			if (!isFirstFile)
				filesJson.Append(",");
			filesJson.AppendFormat(@"
    {{ ""ControlUniqueID"" : ""{0}"", ""FileName"" : ""{1}"", ""ContentType"" : ""{2}"", ""ContentLength"" : {3} }}",
									Quote(file.ControlUniqueID), Quote(file.FileName), Quote(file.ContentType), contentLength);
			isFirstFile = false;
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