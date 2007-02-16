using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;

namespace Brettle.Web.NeatUpload 
{
	public class Global : System.Web.HttpApplication
	{
//#if __MonoCS__ 
//#pragma warning disable 169
//#endif
 		private System.ComponentModel.IContainer components = null;
//#if __MonoCS__ 
//#pragma warning restore 169
//#endif

		public Global()
		{
			InitializeComponent();
		}	
		
		protected void Application_Start(Object sender, EventArgs e)
		{

		}
 
		protected void Session_Start(Object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(Object sender, EventArgs e)
		{
			// This is only used by tests/WithoutNeatUpload.aspx so that it know when the upload
			// starts being received.
			HttpContext.Current.Items["WithoutNeatUpload_StartTime"] = System.DateTime.Now;
		}

		protected void Application_EndRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_Error(Object sender, EventArgs e)
		{
		}

		protected void Session_End(Object sender, EventArgs e)
		{

		}

		protected void Application_End(Object sender, EventArgs e)
		{

		}
			
		#region Web Form Designer generated code
		private void InitializeComponent()
		{    
			this.components = new System.ComponentModel.Container();
			this.BeginRequest += new System.EventHandler(Application_BeginRequest);
		}
		#endregion
	}
}

