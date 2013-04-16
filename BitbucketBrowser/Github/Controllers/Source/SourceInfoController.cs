using System;
using BitbucketBrowser.Controllers;
using BitbucketBrowser.Data;
using GitHubSharp;
using MonoTouch.UIKit;
using BitbucketBrowser.Controllers;
using MonoTouch.Foundation;
using CodeFramework.UI.Views;

namespace BitbucketBrowser.GitHub.Controllers.Source
{
    public class SourceInfoController : FileViewController
    {
        protected string _user, _slug, _branch, _path;
        
        public SourceInfoController(string user, string slug, string branch, string path)
        {
            _user = user;
            _slug = slug;
            _branch = branch;
            _path = path;
            
            //Create the filename
            var fileName = System.IO.Path.GetFileName(path);
            if (fileName == null)
                fileName = path.Substring(path.LastIndexOf('/') + 1);

            //Create the temp file path
            Title = fileName;
        }

        protected override void Request()
        {
            try 
            {
                string mime = "text";
                var file = DownloadFile(_user, _slug, _branch, _path, out mime);
                if (mime.StartsWith("text"))
                    LoadRawData(System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8));
                else
                    LoadFile(file);
            }
            catch (InternalServerException ex)
            {
                MonoTouch.Utilities.ShowAlert("Error", ex.Message);
            }
        }
    }
}

