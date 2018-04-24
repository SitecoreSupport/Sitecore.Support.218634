// --------------------------------------------------------------------------------------------------------------------
// <copyright file="File.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>
// <summary>
//   Represents a File field.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
  using System;
  using Sitecore.Data.Items;
  using Sitecore.IO;
  using Sitecore.Resources.Media;
  using Sitecore.Shell.Framework;
  using Sitecore.Text;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.Diagnostics;
  using Sitecore.Shell.Applications.Dialogs.MediaBrowser;
  using Sitecore.Shell.Applications.ContentEditor;

  /// <summary>
  /// Represents a File field.
  /// </summary>
  public class File : Edit, IContentField
  {
    // <file url="/upload/1.jpg" title="" mediaid="{78E5BAF6-3EAD-43C5-9E95-81A3186991A1}" />

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="File"/> class.
    /// </summary>
    public File()
    {
      this.Class = "scContentControl";
      this.Change = "#";
      this.Activation = true;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    /// <value>The source.</value>
    public string Source
    {
      get
      {
        return this.GetViewStateString("Source");
      }

      set
      {
        string result = MainUtil.UnmapPath(value);

        if (result.EndsWith("/", StringComparison.InvariantCulture))
        {
          result = result.Substring(0, result.Length - 1);
        }

        this.SetViewStateString("Source", result);
      }
    }

    #endregion

    #region Private properties

    /// <summary>
    /// Gets or sets the XML value.
    /// </summary>
    /// <value>The XML value.</value>
    private XmlValue XmlValue
    {
      get
      {
        var result = this.GetViewStateProperty("XmlValue", null) as XmlValue;

        if (result == null)
        {
          result = new XmlValue(string.Empty, "file");
          this.XmlValue = result;
        }

        return result;
      }

      set
      {
        this.SetViewStateProperty("XmlValue", value, null);
      }
    }
    
    #endregion

    #region Public methods

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <returns>
    /// The value of the field.
    /// </returns>
    public string GetValue()
    {
      return this.XmlValue.ToString();
    }

    /// <summary>
    /// Handles the message.
    /// </summary>
    /// <param name="message">The message.</param>
    public override void HandleMessage(Message message)
    {
      string src;

      base.HandleMessage(message);

      if (message["id"] == this.ID)
      {
        switch (message.Name)
        {
          case "contentfile:open":
            Sitecore.Context.ClientPage.Start(this, "OpenFile");
            break;

          case "contentfile:download":
            src = this.XmlValue.GetAttribute("src");

            if (string.IsNullOrEmpty(src))
            {
              Sitecore.Context.ClientPage.ClientResponse.Alert(Texts.THERE_IS_NO_FILE_SELECTED);
              return;
            }

            if (src.StartsWith("~/", StringComparison.InvariantCulture))
            {
              src = FileUtil.MakePath(Sitecore.Context.Site.VirtualFolder, src);
            }

            var url = new UrlString(src);

            Files.Download(url.ToString());
            break;

          case "contentfile:preview":
            src = this.XmlValue.GetAttribute("src");

            if (src.Length > 0)
            {
              Sitecore.Context.ClientPage.ClientResponse.Eval("window.open('" + src + "', '_blank')");
            }
            else
            {
              Sitecore.Context.ClientPage.ClientResponse.Alert(Texts.THERE_IS_NO_FILE_SELECTED);
            }

            break;

          case "contentfile:clear":
            this.ClearFile();
            break;
        }
      }
    }

    /// <summary>
    /// Sets the value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetValue(string value)
    {
      this.XmlValue = new XmlValue(value, "file");

      if (this.XmlValue.GetAttribute("mediaid").Length > 0)
      {
        Item item = Sitecore.Context.ContentDatabase.Items[this.XmlValue.GetAttribute("mediaid")];

        if (item != null)
        {
          this.Value = item.Paths.MediaPath;
        }
      }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.PreRender"></see> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
    protected override void OnPreRender(EventArgs e)
    {
      base.OnPreRender(e);

      // ViewState work-around
      this.ServerProperties["Value"] = this.ServerProperties["Value"];
      this.ServerProperties["XmlValue"] = this.ServerProperties["XmlValue"];
    }

    /// <summary>
    /// Raises the Change event.
    /// </summary>
    /// <param name="message">The message.</param>
    protected override void DoChange(Message message)
    {
      base.DoChange(message);

      this.XmlValue.SetAttribute("mediapath", this.Value);

      string value = this.Value;

      if (!value.StartsWith("/sitecore", StringComparison.InvariantCulture))
      {
        value = Constants.MediaLibraryPath + value;
      }

      MediaItem item = Sitecore.Context.ContentDatabase.Items[value];

      if (item != null)
      {
        MediaUrlOptions options = MediaUrlOptions.GetShellOptions(); 

        string src = MediaManager.GetMediaUrl(item, options);

        this.XmlValue.SetAttribute("mediaid", item.ID.ToString());
        this.XmlValue.SetAttribute("mediapath", item.MediaPath);
        this.XmlValue.SetAttribute("src", src);
      }
      else
      {
        this.XmlValue.SetAttribute("mediaid", string.Empty);
        this.XmlValue.SetAttribute("mediapath", string.Empty);
        this.XmlValue.SetAttribute("src", string.Empty);
      }

      this.SetModified();
      Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
    }

    /// <summary>
    /// Opens the file.
    /// </summary>
    /// <param name="args">The arguments.</param>
    protected void OpenFile(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
        {
          MediaItem item = Sitecore.Context.ContentDatabase.Items[args.Result];

          if (item != null)
          {
            MediaUrlOptions options = MediaUrlOptions.GetShellOptions(); 

            string src = MediaManager.GetMediaUrl(item, options);

            this.XmlValue.SetAttribute("mediaid", item.ID.ToString());
            this.XmlValue.SetAttribute("src", src);

            this.Value = item.MediaPath;

            this.SetModified();
          }
          else
          {
            Sitecore.Context.ClientPage.ClientResponse.Alert(Texts.ITEM_NOT_FOUND);
          }
        }
      }
      else
      {
        string source = StringUtil.GetString(this.Source, Constants.MediaLibraryPath);

        Dialogs.BrowseImage(this.XmlValue.GetAttribute("mediaid"), source, true);

        args.WaitForPostBack();
      }
    }

    /// <summary>
    /// Sets the modified flag.
    /// </summary>
    protected override void SetModified()
    {
      base.SetModified();

      if (this.TrackModified)
      {
        Sitecore.Context.ClientPage.Modified = true;
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Clears the file.
    /// </summary>
    private void ClearFile()
    {
      if (this.Disabled)
      {
        return;
      }

      if (!string.IsNullOrEmpty(this.Value))
      {
        this.SetModified();
      }

      this.XmlValue = new XmlValue(string.Empty, "file");

      this.Value = string.Empty;
    }

    #endregion

    private static void BrowseImage([NotNull] string id, [NotNull] string root, bool ignoreSpeak)
    {
      Assert.ArgumentNotNull(id, "id");
      Assert.ArgumentNotNull(root, "root");

      MediaBrowserOptions options = new MediaBrowserOptions();

      if (string.IsNullOrEmpty(root))
      {
        root = Constants.MediaLibraryPath;
      }

      options.IgnoreSpeak = ignoreSpeak;
      options.Root = Client.ContentDatabase.GetItem(root);

      if (!string.IsNullOrEmpty(id))
      {
        options.SelectedItem = Client.ContentDatabase.GetItem(id);
      }

      SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "1200px", "700px", string.Empty, true);
    }

  }
}